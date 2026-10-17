using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.PostMortem;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents.Workers;

public class PostMortemWorker : RedisAgentWorkerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly PostMortemAgent _agent;
    private readonly ILogger<PostMortemWorker> _logger;

    public override string TargetAgentName => PostMortemAgent.AgentIdentifier;

    public PostMortemWorker(
        IRedisStreamConsumer streamConsumer,
        IIncidentStateStore stateStore,
        IAuditEventWriter auditWriter,
        ILogger<PostMortemWorker> logger,
        PostMortemAgent agent)
        : base(streamConsumer, stateStore, auditWriter, logger)
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
    {
        var report = await _agent.GeneratePostMortemAsync(task.IncidentId, ct).ConfigureAwait(false);

        return new AgentExecutionResult
        {
            AgentName = TargetAgentName,
            Success = true,
            OutputSummary = $"Post-Mortem Generated: '{report.Title}', Duration: {report.TotalDurationMinutes}m, Events: {report.EventsCount}, ActionItems: {report.ActionItems.Count}",
            OutputJson = JsonSerializer.Serialize(report, JsonOptions),
            OutputData = report
        };
    }
}
