using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Trinetra.Core.Interfaces;

namespace Trinetra.Infrastructure.Services;

public class RedisStreamTaskQueue : IAgentTaskQueue, IRedisStreamConsumer
{
    public const string DefaultStreamKey = "trinetra:streams:agent-tasks";
    public const string DefaultConsumerGroup = "trinetra-agents";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisStreamTaskQueue> _logger;

    public RedisStreamTaskQueue(IConnectionMultiplexer redis, ILogger<RedisStreamTaskQueue> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IDatabase Database => _redis.GetDatabase();

    public async Task<string> EnqueueTaskAsync(AgentTaskMessage task, string? streamKey = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        var targetKey = string.IsNullOrWhiteSpace(streamKey) ? DefaultStreamKey : streamKey;

        if (string.IsNullOrEmpty(task.TraceParent) && System.Diagnostics.Activity.Current != null)
        {
            task.TraceParent = System.Diagnostics.Activity.Current.Id;
        }

        var entries = new NameValueEntry[]
        {
            new("taskId", task.TaskId ?? Guid.NewGuid().ToString()),
            new("incidentId", task.IncidentId.ToString()),
            new("agentName", task.AgentName ?? string.Empty),
            new("payload", task.PayloadJson ?? "{}"),
            new("createdAt", task.CreatedAt.ToString("O")),
            new("priority", task.Priority.ToString()),
            new("traceParent", task.TraceParent ?? string.Empty)
        };

        try
        {
            var messageId = await Database.StreamAddAsync(targetKey, entries).ConfigureAwait(false);
            task.MessageId = messageId.ToString();
            _logger.LogDebug("Enqueued task {TaskId} for agent {AgentName} to stream {StreamKey} as {MessageId}",
                task.TaskId, task.AgentName, targetKey, task.MessageId);
            return task.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue task {TaskId} to stream {StreamKey}", task.TaskId, targetKey);
            throw;
        }
    }

    public async Task EnsureConsumerGroupAsync(string streamKey, string consumerGroup, CancellationToken ct = default)
    {
        var targetKey = string.IsNullOrWhiteSpace(streamKey) ? DefaultStreamKey : streamKey;
        var targetGroup = string.IsNullOrWhiteSpace(consumerGroup) ? DefaultConsumerGroup : consumerGroup;

        try
        {
            await Database.StreamCreateConsumerGroupAsync(targetKey, targetGroup, "0-0", createStream: true).ConfigureAwait(false);
            _logger.LogInformation("Created consumer group {ConsumerGroup} for stream {StreamKey}", targetGroup, targetKey);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
        {
            // Consumer group already exists, safe to ignore
            _logger.LogDebug("Consumer group {ConsumerGroup} already exists for stream {StreamKey}", targetGroup, targetKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ensuring consumer group {ConsumerGroup} on stream {StreamKey}", targetGroup, targetKey);
            throw;
        }
    }

    public async Task<IReadOnlyList<AgentTaskMessage>> ReadTasksAsync(string streamKey, string consumerGroup, string consumerName, int count = 10, CancellationToken ct = default)
    {
        var targetKey = string.IsNullOrWhiteSpace(streamKey) ? DefaultStreamKey : streamKey;
        var targetGroup = string.IsNullOrWhiteSpace(consumerGroup) ? DefaultConsumerGroup : consumerGroup;
        var targetConsumer = string.IsNullOrWhiteSpace(consumerName) ? Environment.MachineName : consumerName;

        try
        {
            var entries = await Database.StreamReadGroupAsync(targetKey, targetGroup, targetConsumer, ">", count).ConfigureAwait(false);
            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<AgentTaskMessage>();
            }

            var messages = new List<AgentTaskMessage>(entries.Length);
            foreach (var entry in entries)
            {
                var dict = entry.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString(), StringComparer.OrdinalIgnoreCase);

                var msg = new AgentTaskMessage
                {
                    MessageId = entry.Id.ToString(),
                    TaskId = dict.GetValueOrDefault("taskId", Guid.NewGuid().ToString()),
                    IncidentId = dict.TryGetValue("incidentId", out var incStr) && Guid.TryParse(incStr, out var incId) ? incId : Guid.Empty,
                    AgentName = dict.GetValueOrDefault("agentName", string.Empty),
                    PayloadJson = dict.GetValueOrDefault("payload", "{}"),
                    CreatedAt = dict.TryGetValue("createdAt", out var createdStr) && DateTimeOffset.TryParse(createdStr, out var dto)
                        ? dto
                        : DateTimeOffset.UtcNow,
                    Priority = dict.TryGetValue("priority", out var prioStr) && int.TryParse(prioStr, out var prio)
                        ? prio
                        : 0,
                    TraceParent = dict.TryGetValue("traceParent", out var tp) && !string.IsNullOrEmpty(tp) ? tp : null
                };

                messages.Add(msg);
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read tasks from stream {StreamKey} for consumer group {ConsumerGroup}", targetKey, targetGroup);
            throw;
        }
    }

    public async Task AcknowledgeTaskAsync(string streamKey, string consumerGroup, string messageId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var targetKey = string.IsNullOrWhiteSpace(streamKey) ? DefaultStreamKey : streamKey;
        var targetGroup = string.IsNullOrWhiteSpace(consumerGroup) ? DefaultConsumerGroup : consumerGroup;

        try
        {
            await Database.StreamAcknowledgeAsync(targetKey, targetGroup, messageId).ConfigureAwait(false);
            _logger.LogDebug("Acknowledged message {MessageId} in group {ConsumerGroup} on stream {StreamKey}", messageId, targetGroup, targetKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge message {MessageId} in stream {StreamKey}", messageId, targetKey);
            throw;
        }
    }
}
