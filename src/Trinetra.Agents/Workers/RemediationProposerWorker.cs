using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.RemediationProposer;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents.Workers;

public class RemediationProposerWorker : RedisAgentWorkerBase
{
    private readonly RemediationProposerAgent _agent;

    public override string TargetAgentName => RemediationProposerAgent.AgentIdentifier;

    public RemediationProposerWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<RemediationProposerWorker> logger,
        RemediationProposerAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        string rootCause = "Incident investigation findings";
        string? rootCauseCategory = null;
        string? severity = null;
        string? serviceName = null;
        string? runbooksJson = null;

        if (!string.IsNullOrWhiteSpace(task.PayloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(task.PayloadJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("rootCause", out var rc) || root.TryGetProperty("root_cause", out rc))
                {
                    rootCause = rc.GetString() ?? rootCause;
                }
                if (root.TryGetProperty("rootCauseCategory", out var rcc) || root.TryGetProperty("root_cause_category", out rcc))
                {
                    rootCauseCategory = rcc.GetString();
                }
                if (root.TryGetProperty("severity", out var sv))
                {
                    severity = sv.GetString();
                }
                if (root.TryGetProperty("serviceName", out var sn) || root.TryGetProperty("service", out sn))
                {
                    serviceName = sn.GetString();
                }
                if (root.TryGetProperty("runbooksJson", out var rj) || root.TryGetProperty("runbooks", out rj))
                {
                    runbooksJson = rj.ValueKind == JsonValueKind.String ? rj.GetString() : rj.GetRawText();
                }
            }
            catch { }
        }

        var report = await _agent.ProposeRemediationAsync(
            task.IncidentId,
            rootCause,
            rootCauseCategory,
            severity,
            serviceName,
            runbooksJson,
            ct).ConfigureAwait(false);

        return new AgentExecutionResult
        {
            AgentName = TargetAgentName,
            Success = true,
            OutputSummary = $"Remediation Proposal: Recommended '{report.RecommendedOption?.Title}' (Risk: {report.RecommendedOption?.RiskLevel}, RequiresApproval: {report.RecommendedOption?.RequiresApproval})",
            OutputJson = JsonSerializer.Serialize(report),
            OutputData = report
        };
    }
}
