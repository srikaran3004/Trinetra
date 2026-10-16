using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Agents.Configuration;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Logs;

namespace Trinetra.Agents.LogAnalyzer;

public class LogAnalyzerAgent : AgentBase
{
    public const string AgentIdentifier = "log_analyzer";

    private readonly LogAnalyzerPlugin _plugin;
    private readonly ILogsSnapshotRepository _snapshotRepository;

    public LogAnalyzerAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<LogAnalyzerAgent> logger,
        LogAnalyzerPlugin plugin,
        ILogsSnapshotRepository snapshotRepository,
        IAuditEventWriter? auditWriter = null)
        : base(AgentIdentifier, configuration, kernel, logger, auditWriter)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));

        // Register tools into Semantic Kernel
        RegisterPlugin(_plugin, "LogAnalyzer");
    }

    public async Task<LogAnalysisReport> AnalyzeLogsAsync(
        Guid incidentId,
        string serviceName,
        string timeRange = "15m",
        CancellationToken cancellationToken = default)
    {
        using var activity = StartAgentActivity("analyze_logs");
        if (activity != null)
        {
            activity.SetTag("trinetra.incident.id", incidentId.ToString());
            activity.SetTag("trinetra.service", serviceName);
        }

        var sw = Stopwatch.StartNew();

        // 1. Audit Trail Ground Rule: Record start of analysis
        await RecordAuditEventAsync(
            incidentId,
            "log_analysis_started",
            $"Log Analyzer started log analysis for service '{serviceName}' over time range '{timeRange}'",
            toolCall: "query_logs",
            toolParams: new { serviceName, timeRange },
            executionStatus: "in_progress",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            // 2. Query Logs via Plugin
            var logsJson = await _plugin.QueryLogsAsync(serviceName, timeRange, limit: 1000, cancellationToken)
                .ConfigureAwait(false);

            // 3. Extract Stack Traces
            var stackTracesJson = _plugin.ExtractStackTraces(logsJson);
            var stackTracesResult = JsonSerializer.Deserialize<StackTraceExtractionResult>(stackTracesJson)
                ?? new StackTraceExtractionResult();

            // 4. Detect Anomalies & Error Patterns
            var anomaliesJson = _plugin.DetectAnomalies(logsJson);
            var anomaliesResult = JsonSerializer.Deserialize<LogAnomalyDetectionResult>(anomaliesJson)
                ?? new LogAnomalyDetectionResult();

            // 5. Correlate with Recent Deployments
            var deploymentsJson = _plugin.CorrelateWithDeployment(serviceName, timeRange);
            var deploymentsResult = JsonSerializer.Deserialize<DeploymentCorrelationResult>(deploymentsJson)
                ?? new DeploymentCorrelationResult();

            // Parse raw logs summary
            int totalLogs = 0;
            int errorCount = 0;
            try
            {
                using var doc = JsonDocument.Parse(logsJson);
                if (doc.RootElement.TryGetProperty("count", out var c)) totalLogs = c.GetInt32();
                if (doc.RootElement.TryGetProperty("errors", out var e)) errorCount = e.GetInt32();
            }
            catch { }

            // 6. Build and Persist LogsSnapshot Entity
            var snapshot = new LogsSnapshot
            {
                IncidentId = incidentId,
                Source = "elasticsearch",
                QueryExecuted = $"service:{serviceName} AND timeRange:{timeRange}",
                TimeRangeStart = DateTime.UtcNow - TimeRangeParser.Parse(timeRange),
                TimeRangeEnd = DateTime.UtcNow,
                LogData = logsJson,
                LogCount = totalLogs,
                ErrorPatterns = anomaliesResult.ErrorPatterns,
                StackTraces = stackTracesResult.StackTraces,
                AnomaliesDetected = anomaliesResult.Anomalies,
                SeverityDistribution = JsonSerializer.Serialize(anomaliesResult.SeverityDistribution),
                RetrievedAt = DateTime.UtcNow,
                AnalyzedAt = DateTime.UtcNow
            };

            if (incidentId != Guid.Empty)
            {
                await _snapshotRepository.SaveSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }

            // 7. Formulate Final Report
            var summary = totalLogs == 0
                ? $"No logs found for service '{serviceName}' in time range '{timeRange}'"
                : errorCount > 0
                    ? $"Found {errorCount} errors in {totalLogs} logs for '{serviceName}'. Key patterns: {string.Join(", ", anomaliesResult.ErrorPatterns.Take(2))}"
                    : $"Retrieved {totalLogs} logs for '{serviceName}' with no critical error patterns detected.";

            var report = new LogAnalysisReport
            {
                IncidentId = incidentId,
                Source = "elasticsearch",
                LogsRetrieved = totalLogs,
                ErrorCount = errorCount,
                ErrorPatterns = anomaliesResult.ErrorPatterns,
                StackTraces = stackTracesResult.StackTraces,
                Anomalies = anomaliesResult.Anomalies,
                RelatedDeployments = deploymentsResult.RecentDeployments,
                Summary = summary,
                Confidence = errorCount > 0 ? 0.90m : 0.70m,
                SnapshotId = snapshot.Id
            };

            sw.Stop();

            // 8. Audit Trail Ground Rule: Record completion
            await RecordAuditEventAsync(
                incidentId,
                "log_analysis_completed",
                $"Completed log analysis for '{serviceName}': {summary}",
                toolResult: JsonSerializer.Serialize(report),
                executionStatus: "success",
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Ok);
            return report;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Logger.LogError(ex, "Log Analyzer failed during analysis for service '{ServiceName}', incident {IncidentId}",
                serviceName, incidentId);

            await RecordAuditEventAsync(
                incidentId,
                "log_analysis_failed",
                $"Log analysis failed: {ex.Message}",
                executionStatus: "failed",
                executionError: ex.ToString(),
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
