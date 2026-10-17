using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.RootCauseAnalyzer;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Analysis;

namespace Trinetra.Agents.Workers;

public class RootCauseAnalyzerWorker : RedisAgentWorkerBase
{
    private readonly RootCauseAnalyzerAgent _agent;

    public override string TargetAgentName => RootCauseAnalyzerAgent.AgentIdentifier;

    public RootCauseAnalyzerWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<RootCauseAnalyzerWorker> logger,
        RootCauseAnalyzerAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        string? incidentSummary = null;
        string? logsJson = null;
        string? metricsJson = null;
        string? runbooksJson = null;

        if (!string.IsNullOrWhiteSpace(task.PayloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(task.PayloadJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("incidentSummary", out var ism) || root.TryGetProperty("summary", out ism))
                {
                    incidentSummary = ism.GetString();
                }
                if (root.TryGetProperty("logsJson", out var lj) || root.TryGetProperty("logs", out lj))
                {
                    logsJson = lj.ValueKind == JsonValueKind.String ? lj.GetString() : lj.GetRawText();
                }
                if (root.TryGetProperty("metricsJson", out var mj) || root.TryGetProperty("metrics", out mj))
                {
                    metricsJson = mj.ValueKind == JsonValueKind.String ? mj.GetString() : mj.GetRawText();
                }
                if (root.TryGetProperty("runbooksJson", out var rj) || root.TryGetProperty("runbooks", out rj))
                {
                    runbooksJson = rj.ValueKind == JsonValueKind.String ? rj.GetString() : rj.GetRawText();
                }
            }
            catch { }
        }

        var report = await _agent.AnalyzeRootCauseAsync(task.IncidentId, incidentSummary, logsJson, metricsJson, runbooksJson, ct)
            .ConfigureAwait(false);

        return new AgentExecutionResult
        {
            AgentName = TargetAgentName,
            Success = true,
            OutputSummary = $"Root Cause: [{report.RootCauseCategory}] {report.RootCause} ({report.ConfidenceScore:P0})",
            OutputJson = JsonSerializer.Serialize(report),
            OutputData = report
        };
    }
}
