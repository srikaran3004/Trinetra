using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents.Coordinator;

public class InvestigationReport
{
    public Guid IncidentId { get; set; }
    public bool Success { get; set; }
    public Dictionary<string, AgentExecutionResult> AgentResults { get; set; } = new();
    public string Status { get; set; } = IncidentStatuses.Triggered;
    public long TotalDurationMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class IncidentCoordinatorAgent : AgentBase
{
    private readonly IIncidentCoordinatorStore _store;
    private readonly IAgentInvoker _agentInvoker;
    private readonly CoordinatorPlugin _plugin;

    public IncidentCoordinatorAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<IncidentCoordinatorAgent> logger,
        IIncidentCoordinatorStore store,
        IAgentInvoker agentInvoker,
        CoordinatorPlugin plugin,
        IAuditEventWriter? auditWriter = null)
        : base("IncidentCoordinator", configuration, kernel, logger, auditWriter)
    {
        _store = store;
        _agentInvoker = agentInvoker;
        _plugin = plugin;

        // Register tools into Semantic Kernel
        RegisterPlugin(_plugin, "CoordinatorPlugin");
    }

    /// <summary>
    /// Executes the complete Phase 1 multi-agent parallel investigation workflow:
    /// Alert -> Triggered -> Investigating (Parallel: LogAnalyzer, MetricsQuerier, RunbookRetriever) -> AnalysisComplete
    /// </summary>
    public async Task<InvestigationReport> CoordinateInvestigationAsync(
        Guid incidentId,
        int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var report = new InvestigationReport { IncidentId = incidentId };

        Logger.LogInformation("Incident Coordinator starting investigation for incident {IncidentId}", incidentId);

        var incident = await _store.GetIncidentAsync(incidentId, cancellationToken);
        if (incident == null)
        {
            report.Success = false;
            report.ErrorMessage = $"Incident with ID '{incidentId}' not found.";
            return report;
        }

        try
        {
            // 1. Transition state from Triggered to Investigating
            var transitionReason = "Initiating parallel multi-agent investigation (LogAnalyzer, MetricsQuerier, RunbookRetriever)";
            await _plugin.UpdateIncidentStatusAsync(incidentId.ToString(), IncidentStatuses.Investigating, transitionReason, cancellationToken);

            await RecordAuditEventAsync(
                incidentId,
                eventType: "investigation_started",
                actionDescription: transitionReason,
                toolCall: "CoordinateInvestigation",
                cancellationToken: cancellationToken);

            // 2. Parallel fan-out: Invoke 3 core investigation agents simultaneously
            var serviceName = incident.AffectedServices?.FirstOrDefault() ?? "api";
            var summary = !string.IsNullOrWhiteSpace(incident.Description) ? incident.Description : incident.Title;

            var parallelAgents = new[] { "log_analyzer", "metrics_querier", "runbook_retriever" };

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, timeoutSeconds)));

            var tasks = parallelAgents.Select(async agentName =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    var req = new AgentInvocationRequest
                    {
                        IncidentId = incidentId,
                        AgentName = agentName,
                        Action = "investigate",
                        TimeoutSeconds = timeoutSeconds,
                        Parameters = new Dictionary<string, object?>
                        {
                            ["serviceName"] = serviceName,
                            ["service"] = serviceName,
                            ["incidentSummary"] = summary,
                            ["summary"] = summary,
                            ["timeRange"] = "15m",
                            ["incidentStartTime"] = incident.TriggeredAt.ToString("O"),
                            ["affected_services"] = incident.AffectedServices,
                            ["severity"] = incident.Severity,
                            ["triggered_at"] = incident.TriggeredAt
                        }
                    };

                    var result = await _agentInvoker.InvokeAgentAsync(req, linkedCts.Token);
                    sw.Stop();
                    result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                    return result;
                }
                catch (OperationCanceledException)
                {
                    sw.Stop();
                    return AgentExecutionResult.Failed(agentName, $"Agent execution timed out after {timeoutSeconds}s", sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    Logger.LogError(ex, "Agent '{AgentName}' failed during investigation for incident {IncidentId}", agentName, incidentId);
                    return AgentExecutionResult.Failed(agentName, ex.Message, sw.ElapsedMilliseconds);
                }
            }).ToList();

            // Fan-in: Wait for all parallel agents to complete
            var results = await Task.WhenAll(tasks);
            foreach (var res in results)
            {
                report.AgentResults[res.AgentName] = res;
            }

            int successfulCount = report.AgentResults.Values.Count(r => r.Success);
            int failedCount = report.AgentResults.Values.Count(r => !r.Success);

            if (successfulCount == 0 && parallelAgents.Length > 0)
            {
                var failureReason = $"All parallel investigation agents failed ({string.Join(", ", report.AgentResults.Select(kv => $"{kv.Key}: {kv.Value.ErrorMessage}"))})";
                await _plugin.EscalateIncidentAsync(incidentId.ToString(), failureReason, cancellationToken);
                report.Success = false;
                report.Status = IncidentStatuses.Escalated;
                report.ErrorMessage = failureReason;
                report.TotalDurationMs = stopwatch.ElapsedMilliseconds;
                return report;
            }

            // 3. Transition to AnalysisComplete (with partial results support)
            var completionReason = failedCount > 0
                ? $"Parallel investigation completed with partial results ({successfulCount}/{parallelAgents.Length} agents succeeded). Ready for Root Cause Analysis."
                : "All parallel investigation agents completed. Ready for Root Cause Analysis.";

            await _plugin.UpdateIncidentStatusAsync(
                incidentId.ToString(),
                IncidentStatuses.AnalysisComplete,
                completionReason,
                cancellationToken);

            await RecordAuditEventAsync(
                incidentId,
                eventType: "investigation_completed",
                actionDescription: completionReason,
                toolCall: "CoordinateInvestigation",
                toolResult: JsonSerializer.Serialize(new
                {
                    successfulAgents = report.AgentResults.Where(kv => kv.Value.Success).Select(kv => kv.Key),
                    failedAgents = report.AgentResults.Where(kv => !kv.Value.Success).Select(kv => kv.Key),
                    partialResults = failedCount > 0
                }),
                executionStatus: ExecutionStatuses.Success,
                executionTimeMs: (int)stopwatch.ElapsedMilliseconds,
                cancellationToken: cancellationToken);

            stopwatch.Stop();
            report.Success = true;
            report.Status = IncidentStatuses.AnalysisComplete;
            report.TotalDurationMs = stopwatch.ElapsedMilliseconds;

            Logger.LogInformation("Investigation completed for incident {IncidentId} ({SuccessCount}/{Total} succeeded) in {ElapsedMs}ms",
                incidentId, successfulCount, parallelAgents.Length, report.TotalDurationMs);
            return report;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Investigation coordination failed for incident {IncidentId}", incidentId);

            // Escalate on failure
            await _plugin.EscalateIncidentAsync(incidentId.ToString(), $"Investigation failed: {ex.Message}", cancellationToken);

            report.Success = false;
            report.Status = IncidentStatuses.Escalated;
            report.ErrorMessage = ex.Message;
            report.TotalDurationMs = stopwatch.ElapsedMilliseconds;
            return report;
        }
    }
}
