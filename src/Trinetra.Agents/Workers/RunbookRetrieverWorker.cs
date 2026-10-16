using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.RunbookRetriever;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Runbooks;

namespace Trinetra.Agents.Workers;

public class RunbookRetrieverWorker : RedisAgentWorkerBase
{
    private readonly RunbookRetrieverAgent _agent;

    public override string TargetAgentName => RunbookRetrieverAgent.AgentIdentifier;

    public RunbookRetrieverWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<RunbookRetrieverWorker> logger,
        RunbookRetrieverAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        string incidentSummary = "Incident reported";
        string? serviceName = null;
        List<string>? errorPatterns = null;

        if (!string.IsNullOrWhiteSpace(task.PayloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(task.PayloadJson);
                if (doc.RootElement.TryGetProperty("incidentSummary", out var sm) || doc.RootElement.TryGetProperty("summary", out sm))
                {
                    incidentSummary = sm.GetString() ?? incidentSummary;
                }
                if (doc.RootElement.TryGetProperty("serviceName", out var s) || doc.RootElement.TryGetProperty("service", out s))
                {
                    serviceName = s.GetString();
                }
                if (doc.RootElement.TryGetProperty("errorPatterns", out var ep) && ep.ValueKind == JsonValueKind.Array)
                {
                    errorPatterns = new List<string>();
                    foreach (var item in ep.EnumerateArray())
                    {
                        var str = item.GetString();
                        if (!string.IsNullOrWhiteSpace(str)) errorPatterns.Add(str);
                    }
                }
            }
            catch { }
        }

        var report = await _agent.RetrieveRunbooksAsync(task.IncidentId, incidentSummary, errorPatterns, serviceName, ct)
            .ConfigureAwait(false);

        return new AgentExecutionResult
        {
            AgentName = TargetAgentName,
            Success = true,
            OutputSummary = report.Summary,
            OutputJson = JsonSerializer.Serialize(report),
            OutputData = report
        };
    }
}
