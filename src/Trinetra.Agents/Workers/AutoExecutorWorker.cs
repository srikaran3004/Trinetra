using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.AutoExecutor;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Execution;

namespace Trinetra.Agents.Workers;

public class AutoExecutorWorker : RedisAgentWorkerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AutoExecutorAgent _agent;
    private readonly ILogger<AutoExecutorWorker> _logger;

    public override string TargetAgentName => AutoExecutorAgent.AgentIdentifier;

    public AutoExecutorWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<AutoExecutorWorker> logger,
        AutoExecutorAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        CommandExecutionRequest? request = null;

        if (!string.IsNullOrWhiteSpace(task.PayloadJson))
        {
            try
            {
                request = JsonSerializer.Deserialize<CommandExecutionRequest>(task.PayloadJson, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse CommandExecutionRequest from task payload for incident {IncidentId}", task.IncidentId);
            }
        }

        request ??= new CommandExecutionRequest
        {
            IncidentId = task.IncidentId,
            ActionTitle = "Automated Remediation",
            RiskLevel = "low",
            IsApproved = false
        };

        var result = await _agent.ExecuteRemediationAsync(request, ct).ConfigureAwait(false);

        return new AgentExecutionResult
        {
            AgentName = TargetAgentName,
            Success = result.Status == ExecutionStatus.Success,
            OutputSummary = $"Remediation Execution: Status={result.Status}, Steps={result.ExecutionSteps.Count}, Duration={result.TotalDurationSeconds}s",
            OutputJson = JsonSerializer.Serialize(result, JsonOptions),
            OutputData = result,
            ErrorMessage = result.ErrorMessage
        };
    }
}
