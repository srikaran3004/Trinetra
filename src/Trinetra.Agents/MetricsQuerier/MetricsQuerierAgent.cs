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
using Trinetra.Core.Models.Metrics;

namespace Trinetra.Agents.MetricsQuerier;

public class MetricsQuerierAgent : AgentBase
{
    public const string AgentIdentifier = "metrics_querier";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly MetricsQuerierPlugin _plugin;
    private readonly IMetricsSnapshotRepository _snapshotRepository;

    public MetricsQuerierAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<MetricsQuerierAgent> logger,
        MetricsQuerierPlugin plugin,
        IMetricsSnapshotRepository snapshotRepository,
        IAuditEventWriter? auditWriter = null)
        : base(AgentIdentifier, configuration, kernel, logger, auditWriter)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));

        // Register tools into Semantic Kernel
        RegisterPlugin(_plugin, "MetricsQuerier");
    }

    public async Task<MetricsAnalysisReport> QueryAndAnalyzeMetricsAsync(
        Guid incidentId,
        string serviceName = "api",
        string timeRange = "15m",
        DateTime? incidentStartTime = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartAgentActivity("query_and_analyze_metrics");
        if (activity != null)
        {
            activity.SetTag("trinetra.incident.id", incidentId.ToString());
            activity.SetTag("trinetra.service", serviceName);
        }

        var sw = Stopwatch.StartNew();

        // 1. Audit Trail Ground Rule: Record start of analysis
        await RecordAuditEventAsync(
            incidentId,
            "metrics_query_started",
            $"Metrics Querier started infrastructure metrics collection and saturation analysis for service '{serviceName}' over range '{timeRange}'",
            toolCall: "query_metrics",
            toolParams: new { serviceName, timeRange, incidentStartTime },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            var defaultMetrics = new[] { "cpu_percent", "memory_percent", "database_connections", "disk_io_read_mbps" };
            var metricsCsv = string.Join(",", defaultMetrics);

            // 2. Query Metrics
            var metricsJson = await _plugin.QueryMetricsAsync(metricsCsv, timeRange, "avg", cancellationToken)
                .ConfigureAwait(false);

            // 3. Detect Saturation
            var saturationJson = await _plugin.DetectSaturationAsync(serviceName, 90.0, metricsJson, cancellationToken)
                .ConfigureAwait(false);

            var saturationResult = JsonSerializer.Deserialize<ResourceSaturationResult>(saturationJson, JsonOptions)
                ?? new ResourceSaturationResult();

            // 4. Correlate with Incident Time
            var startIso = incidentStartTime?.ToString("O");
            var correlationJson = _plugin.CorrelateWithIncidentTime(metricsJson, startIso);

            var correlationResult = JsonSerializer.Deserialize<CorrelationAnalysisResult>(correlationJson, JsonOptions)
                ?? new CorrelationAnalysisResult();

            // 5. Build Anomalies and Summary
            var anomalies = new List<string>();
            foreach (var spike in saturationResult.Spikes)
            {
                anomalies.Add(spike);
            }

            var duration = TimeRangeParser.Parse(timeRange);
            var end = DateTime.UtcNow;
            var start = end - duration;

            var summary = BuildSummary(serviceName, saturationResult, correlationResult);
            var confidence = saturationResult.SaturatedResources.Count > 0 ? 0.95m : 0.88m;

            // 6. Build and Persist MetricsSnapshot Entity
            var snapshot = new MetricsSnapshot
            {
                IncidentId = incidentId,
                Source = "prometheus",
                QueryExecuted = $"service:{serviceName} AND metrics:[{metricsCsv}] AND timeRange:{timeRange}",
                TimeRangeStart = start,
                TimeRangeEnd = end,
                MetricsData = metricsJson,
                MetricNames = defaultMetrics.ToList(),
                AnomaliesDetected = anomalies,
                ResourceSaturation = JsonSerializer.Serialize(saturationResult.ResourceSaturation),
                BaselineDeviations = JsonSerializer.Serialize(saturationResult.Spikes),
                RetrievedAt = DateTime.UtcNow,
                AnalyzedAt = DateTime.UtcNow
            };

            if (incidentId != Guid.Empty)
            {
                await _snapshotRepository.SaveSnapshotAsync(snapshot, cancellationToken).ConfigureAwait(false);
            }

            var report = new MetricsAnalysisReport
            {
                IncidentId = incidentId,
                MetricsSource = "prometheus",
                TimeRangeStart = start,
                TimeRangeEnd = end,
                ResourceSaturation = saturationResult.ResourceSaturation,
                Anomalies = anomalies,
                CorrelationAnalysis = correlationResult,
                Summary = summary,
                Confidence = confidence,
                SnapshotId = snapshot.Id
            };

            sw.Stop();

            // 7. Audit Trail Ground Rule: Record completion
            await RecordAuditEventAsync(
                incidentId,
                "metrics_query_completed",
                $"Completed metrics analysis for '{serviceName}': {summary}",
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
            Logger.LogError(ex, "Metrics Querier failed during analysis for service '{ServiceName}', incident {IncidentId}",
                serviceName, incidentId);

            await RecordAuditEventAsync(
                incidentId,
                "metrics_query_failed",
                $"Metrics analysis failed: {ex.Message}",
                executionStatus: "failed",
                executionError: ex.ToString(),
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static string BuildSummary(string service, ResourceSaturationResult saturation, CorrelationAnalysisResult correlation)
    {
        if (saturation.SaturatedResources.Count > 0)
        {
            var saturatedStr = string.Join(", ", saturation.SaturatedResources);
            var leadStr = correlation.MetricsLedIncident ? "Metrics degradation preceded incident detection." : "Metrics spiked synchronously with incident.";
            return $"{service}: Resource saturation detected on [{saturatedStr}]. {leadStr}";
        }

        if (saturation.Spikes.Count > 0)
        {
            return $"{service}: Metric spikes observed above 95th percentile baseline with no complete saturation breach.";
        }

        return $"{service}: Infrastructure metrics within normal operational bounds.";
    }
}
