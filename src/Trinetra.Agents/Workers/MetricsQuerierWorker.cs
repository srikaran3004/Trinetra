using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.MetricsQuerier;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Metrics;

namespace Trinetra.Agents.Workers;

public class MetricsQuerierWorker : RedisAgentWorkerBase
{
    private readonly MetricsQuerierAgent _agent;

    public override string TargetAgentName => MetricsQuerierAgent.AgentIdentifier;

    public MetricsQuerierWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<MetricsQuerierWorker> logger,
        MetricsQuerierAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        string serviceName = "default-service";
        string timeRange = "15m";
        DateTime? incidentStartTime = null;

        if (!string.IsNullOrWhiteSpace(task.PayloadJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(task.PayloadJson);
                if (doc.RootElement.TryGetProperty("serviceName", out var s) || doc.RootElement.TryGetProperty("service", out s))
                {
                    serviceName = s.GetString() ?? serviceName;
                }
                if (doc.RootElement.TryGetProperty("timeRange", out var tr))
                {
                    timeRange = tr.GetString() ?? timeRange;
                }
                if (doc.RootElement.TryGetProperty("incidentStartTime", out var st) &&
                    DateTime.TryParse(st.GetString(), out var parsedTime))
                {
                    incidentStartTime = parsedTime;
                }
            }
            catch { }
        }

        var report = await _agent.QueryAndAnalyzeMetricsAsync(task.IncidentId, serviceName, timeRange, incidentStartTime, ct)
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
