using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.LogAnalyzer;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Logs;

namespace Trinetra.Agents.Workers;

public class LogAnalyzerWorker : RedisAgentWorkerBase
{
    private readonly LogAnalyzerAgent _agent;

    public override string TargetAgentName => LogAnalyzerAgent.AgentIdentifier;

    public LogAnalyzerWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<LogAnalyzerWorker> logger,
        LogAnalyzerAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        string serviceName = "default-service";
        string timeRange = "15m";

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
            }
            catch { }
        }

        var report = await _agent.AnalyzeLogsAsync(task.IncidentId, serviceName, timeRange, ct).ConfigureAwait(false);

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
