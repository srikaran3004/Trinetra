using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents.Workers;

public abstract class RedisAgentWorkerBase : BackgroundService
{
    private readonly IRedisStreamConsumer _streamConsumer;
    private readonly IIncidentStateStore _stateStore;
    private readonly IAuditEventWriter _auditWriter;
    private readonly ILogger _logger;

    protected RedisAgentWorkerBase(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger logger)
    {
        _streamConsumer = streamConsumer ?? throw new ArgumentNullException(nameof(streamConsumer));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _auditWriter = auditWriter ?? throw new ArgumentNullException(nameof(auditWriter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public virtual string StreamKey => "trinetra:streams:agent-tasks";
    public virtual string ConsumerGroup => "trinetra-agents";
    public virtual string ConsumerName => $"{GetType().Name}-{Environment.MachineName}";
    public abstract string TargetAgentName { get; }

    protected abstract Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RedisAgentWorker {WorkerName} starting for target agent {TargetAgent}",
            ConsumerName, TargetAgentName);

        try
        {
            await _streamConsumer.EnsureConsumerGroupAsync(StreamKey, ConsumerGroup, stoppingToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Could not ensure consumer group {Group} on stream {Stream}; will retry in loop",
                ConsumerGroup, StreamKey);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await _streamConsumer.ReadTasksAsync(StreamKey, ConsumerGroup, ConsumerName, count: 5, stoppingToken)
                    .ConfigureAwait(false);

                if (messages == null || messages.Count == 0)
                {
                    await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
                    continue;
                }

                foreach (var task in messages)
                {
                    if (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // Check if message belongs to this worker
                    if (!string.Equals(TargetAgentName, "*", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(task.AgentName, TargetAgentName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    await ProcessSingleTaskAsync(task, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in RedisAgentWorker {WorkerName} loop", ConsumerName);
                await Task.Delay(2000, stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("RedisAgentWorker {WorkerName} stopped", ConsumerName);
    }

    private async Task ProcessSingleTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        ActivityContext parentContext = default;
        if (!string.IsNullOrEmpty(task.TraceParent) && ActivityContext.TryParse(task.TraceParent, null, out var parsed))
        {
            parentContext = parsed;
        }

        using var activity = Trinetra.Core.Tracing.TrinetraActivitySource.Source.StartActivity(
            $"AgentWorker {task.AgentName}",
            ActivityKind.Consumer,
            parentContext);

        if (activity != null)
        {
            activity.SetTag("trinetra.agent.name", task.AgentName);
            activity.SetTag("trinetra.incident.id", task.IncidentId.ToString());
            activity.SetTag("trinetra.task.id", task.TaskId);
            activity.SetTag("trinetra.priority", task.Priority);
        }

        _logger.LogInformation("Processing task {TaskId} for agent {AgentName} on incident {IncidentId}",
            task.TaskId, task.AgentName, task.IncidentId);

        // Update state in Redis to 'running'
        var agentState = new AgentStateSnapshot
        {
            AgentName = task.AgentName,
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow
        };

        if (task.IncidentId != Guid.Empty)
        {
            await _stateStore.SetAgentStatusAsync(task.IncidentId, agentState, ct: ct).ConfigureAwait(false);
            await _auditWriter.WriteEventAsync(new IncidentEvent
            {
                IncidentId = task.IncidentId,
                AgentName = task.AgentName,
                EventType = "agent_task_started",
                ActionDescription = $"Started processing task {task.TaskId}",
                ToolParams = JsonSerializer.Serialize(agentState),
                ExecutionStatus = "in_progress"
            }, ct).ConfigureAwait(false);
        }

        try
        {
            var result = await ProcessTaskAsync(task, ct).ConfigureAwait(false);

            if (activity != null)
            {
                activity.SetStatus(result.Success ? ActivityStatusCode.Ok : ActivityStatusCode.Error, result.ErrorMessage);
                if (!result.Success) activity.SetTag("error.message", result.ErrorMessage);
            }

            var completedState = new AgentStateSnapshot
            {
                AgentName = task.AgentName,
                Status = result.Success ? "completed" : "failed",
                StartedAt = agentState.StartedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = result.ErrorMessage,
                OutputSummary = result.OutputSummary
            };

            if (task.IncidentId != Guid.Empty)
            {
                await _stateStore.SetAgentStatusAsync(task.IncidentId, completedState, ct: ct).ConfigureAwait(false);
                await _auditWriter.WriteEventAsync(new IncidentEvent
                {
                    IncidentId = task.IncidentId,
                    AgentName = task.AgentName,
                    EventType = result.Success ? "agent_task_completed" : "agent_task_failed",
                    ActionDescription = result.Success
                        ? $"Completed task {task.TaskId}: {result.OutputSummary}"
                        : $"Task {task.TaskId} failed: {result.ErrorMessage}",
                    ToolResult = result.OutputJson ?? (result.OutputData != null ? JsonSerializer.Serialize(result.OutputData) : null),
                    ExecutionStatus = result.Success ? "success" : "failed",
                    ExecutionError = result.ErrorMessage,
                    ExecutionTimeMs = (int)result.ExecutionTimeMs
                }, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(task.MessageId))
            {
                await _streamConsumer.AcknowledgeTaskAsync(StreamKey, ConsumerGroup, task.MessageId, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute task {TaskId} for agent {AgentName}", task.TaskId, task.AgentName);

            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.SetTag("error.message", ex.Message);
            }

            var failedState = new AgentStateSnapshot
            {
                AgentName = task.AgentName,
                Status = "failed",
                StartedAt = agentState.StartedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message
            };

            if (task.IncidentId != Guid.Empty)
            {
                await _stateStore.SetAgentStatusAsync(task.IncidentId, failedState, ct: ct).ConfigureAwait(false);
                await _auditWriter.WriteEventAsync(new IncidentEvent
                {
                    IncidentId = task.IncidentId,
                    AgentName = task.AgentName,
                    EventType = "agent_task_failed",
                    ActionDescription = $"Task {task.TaskId} threw an unhandled exception: {ex.Message}",
                    ExecutionStatus = "failed",
                    ExecutionError = ex.ToString()
                }, ct).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(task.MessageId))
            {
                await _streamConsumer.AcknowledgeTaskAsync(StreamKey, ConsumerGroup, task.MessageId, ct).ConfigureAwait(false);
            }
        }
    }
}
