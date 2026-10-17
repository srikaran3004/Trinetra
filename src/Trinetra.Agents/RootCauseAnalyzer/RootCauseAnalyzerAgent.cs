using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Coordinator;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Analysis;

namespace Trinetra.Agents.RootCauseAnalyzer;

public class RootCauseAnalyzerAgent : AgentBase
{
    public const string AgentIdentifier = "root_cause_analyzer";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RootCauseAnalyzerPlugin _plugin;
    private readonly IIncidentCoordinatorStore _store;
    private readonly ILogsSnapshotRepository? _logsSnapshotRepo;
    private readonly IMetricsSnapshotRepository? _metricsSnapshotRepo;

    public RootCauseAnalyzerAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<RootCauseAnalyzerAgent> logger,
        RootCauseAnalyzerPlugin plugin,
        IIncidentCoordinatorStore store,
        ILogsSnapshotRepository? logsSnapshotRepo = null,
        IMetricsSnapshotRepository? metricsSnapshotRepo = null,
        IAuditEventWriter? auditWriter = null)
        : base(AgentIdentifier, configuration, kernel, logger, auditWriter)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logsSnapshotRepo = logsSnapshotRepo;
        _metricsSnapshotRepo = metricsSnapshotRepo;

        // Register tools into Semantic Kernel
        RegisterPlugin(_plugin, "RootCauseAnalyzer");
    }

    public async Task<RootCauseReport> AnalyzeRootCauseAsync(
        Guid incidentId,
        string? incidentSummary = null,
        string? logsJson = null,
        string? metricsJson = null,
        string? runbooksJson = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartAgentActivity("analyze_root_cause");
        if (activity != null)
        {
            activity.SetTag("trinetra.incident.id", incidentId.ToString());
            activity.SetTag("trinetra.summary", incidentSummary ?? "unknown");
        }

        var sw = Stopwatch.StartNew();

        // 1. Audit Trail Ground Rule: Record start of analysis
        await RecordAuditEventAsync(
            incidentId,
            "root_cause_analysis_started",
            $"Root Cause Analyzer initiated multi-modal synthesis for incident {incidentId}",
            toolCall: "correlate_evidence",
            toolParams: new { incidentSummary },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            // 2. Fetch logs snapshot if not passed explicitly
            if (string.IsNullOrWhiteSpace(logsJson) && _logsSnapshotRepo != null)
            {
                var logsSnapshot = await _logsSnapshotRepo.GetSnapshotByIncidentIdAsync(incidentId, cancellationToken)
                    .ConfigureAwait(false);
                if (logsSnapshot != null)
                {
                    logsJson = JsonSerializer.Serialize(new
                    {
                        error_patterns = logsSnapshot.ErrorPatterns,
                        stack_traces = logsSnapshot.StackTraces,
                        anomalies = logsSnapshot.AnomaliesDetected,
                        log_data = logsSnapshot.LogData
                    });
                }
            }

            // 3. Fetch metrics snapshot if not passed explicitly
            if (string.IsNullOrWhiteSpace(metricsJson) && _metricsSnapshotRepo != null)
            {
                var metricsSnapshot = await _metricsSnapshotRepo.GetSnapshotByIncidentIdAsync(incidentId, cancellationToken)
                    .ConfigureAwait(false);
                if (metricsSnapshot != null)
                {
                    object? saturationObj = null;
                    if (!string.IsNullOrWhiteSpace(metricsSnapshot.ResourceSaturation))
                    {
                        try { saturationObj = JsonDocument.Parse(metricsSnapshot.ResourceSaturation).RootElement; } catch { }
                    }

                    metricsJson = JsonSerializer.Serialize(new
                    {
                        anomalies = metricsSnapshot.AnomaliesDetected,
                        resource_saturation = saturationObj,
                        metrics_data = metricsSnapshot.MetricsData
                    });
                }
            }

            // 4. Correlate evidence across modalities
            var correlatedJson = _plugin.CorrelateEvidence(logsJson, metricsJson, runbooksJson);

            // 5. Classify root cause category
            var classificationJson = _plugin.ClassifyRootCause(correlatedJson);

            // 6. Calculate confidence score
            var confidenceJson = _plugin.CalculateConfidence(correlatedJson);

            // 7. Assemble report
            var report = new RootCauseReport
            {
                IncidentId = incidentId,
                RootCauseCategory = RootCauseCategories.Unknown,
                ConfidenceScore = 0.88m
            };

            // Parse correlated output
            using (var doc = JsonDocument.Parse(correlatedJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("evidence_chain", out var ec))
                {
                    report.EvidenceChain = JsonSerializer.Deserialize<EvidenceChain>(ec.GetRawText(), JsonOptions) ?? new();
                }
                if (root.TryGetProperty("contributing_factors", out var cf) && cf.ValueKind == JsonValueKind.Array)
                {
                    report.ContributingFactors = JsonSerializer.Deserialize<List<string>>(cf.GetRawText(), JsonOptions) ?? new();
                }
                if (root.TryGetProperty("similar_incidents", out var si) && si.ValueKind == JsonValueKind.Array)
                {
                    report.SimilarIncidents = JsonSerializer.Deserialize<List<SimilarIncidentReference>>(si.GetRawText(), JsonOptions) ?? new();
                }
            }

            // Parse classification
            using (var doc = JsonDocument.Parse(classificationJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("root_cause_category", out var rcc))
                {
                    report.RootCauseCategory = rcc.GetString() ?? RootCauseCategories.Unknown;
                }
                if (root.TryGetProperty("root_cause", out var rc))
                {
                    report.RootCause = rc.GetString() ?? "Undetermined root cause";
                }
            }

            // Parse confidence
            using (var doc = JsonDocument.Parse(confidenceJson))
            {
                var root = doc.RootElement;
                if (root.TryGetProperty("confidence_score", out var cs))
                {
                    report.ConfidenceScore = (decimal)cs.GetDouble();
                }
            }

            // 8. Update database findings
            await _store.UpdateIncidentFindingsAsync(
                incidentId,
                report.RootCause,
                report.RootCauseCategory,
                report.ConfidenceScore,
                proposedRemediation: null,
                riskLevel: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            sw.Stop();

            // 9. Audit Trail Ground Rule: Record completion
            await RecordAuditEventAsync(
                incidentId,
                "root_cause_analysis_completed",
                $"Root Cause identified: [{report.RootCauseCategory}] {report.RootCause} (Confidence: {report.ConfidenceScore:P0})",
                toolCall: "classify_root_cause",
                toolResult: JsonSerializer.Serialize(report),
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Logger.LogInformation(
                "Root Cause Analysis complete for {IncidentId}: Category={Category}, Confidence={Confidence}, Duration={Elapsed}ms",
                incidentId, report.RootCauseCategory, report.ConfidenceScore, sw.ElapsedMilliseconds);

            return report;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Logger.LogError(ex, "Failed to analyze root cause for incident {IncidentId}", incidentId);

            await RecordAuditEventAsync(
                incidentId,
                "root_cause_analysis_failed",
                $"Root Cause Analyzer encountered an error: {ex.Message}",
                executionStatus: "failed",
                executionError: ex.Message,
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw;
        }
    }
}
