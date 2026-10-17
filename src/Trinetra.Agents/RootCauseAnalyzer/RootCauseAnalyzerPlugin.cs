using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Models.Analysis;
using Trinetra.Core.Models.Logs;
using Trinetra.Core.Models.Metrics;
using Trinetra.Core.Models.Runbooks;

namespace Trinetra.Agents.RootCauseAnalyzer;

public class RootCauseAnalyzerPlugin
{
    private readonly ILogger<RootCauseAnalyzerPlugin> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RootCauseAnalyzerPlugin(ILogger<RootCauseAnalyzerPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("correlate_evidence")]
    [Description("Synthesizes multi-modal findings from logs, metrics, and runbooks into an evidence chain")]
    public string CorrelateEvidence(
        [Description("Log analysis report or snapshot JSON string")] string? logsJson,
        [Description("Metrics analysis report or snapshot JSON string")] string? metricsJson,
        [Description("Runbook retrieval report JSON string")] string? runbooksJson)
    {
        _logger.LogInformation("Tool correlate_evidence invoked");

        var chain = new EvidenceChain();
        var contributingFactors = new List<string>();

        // 1. Process Logs Evidence
        if (!string.IsNullOrWhiteSpace(logsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(logsJson);
                var root = doc.RootElement;

                // Check for recent deployments
                if (root.TryGetProperty("related_deployments", out var depArr) && depArr.ValueKind == JsonValueKind.Array && depArr.GetArrayLength() > 0)
                {
                    var firstDep = depArr[0];
                    var svc = firstDep.TryGetProperty("service", out var s) ? s.GetString() : "service";
                    var ver = firstDep.TryGetProperty("version", out var v) ? v.GetString() : "new version";
                    var files = firstDep.TryGetProperty("changed_files", out var cf) && cf.ValueKind == JsonValueKind.Array
                        ? string.Join(", ", cf.EnumerateArray().Select(f => f.GetString()))
                        : "code files";

                    chain.DeploymentEvidence = new EvidenceItem
                    {
                        Source = "git_history",
                        Detail = $"{svc} {ver} deployed recently with changes to [{files}]",
                        Confidence = 0.95
                    };
                    contributingFactors.Add($"{svc} {ver} deployed recently with changed files: {files}");
                }

                // Check for errors and stack traces
                var errorPatterns = new List<string>();
                if (root.TryGetProperty("error_patterns", out var ep) && ep.ValueKind == JsonValueKind.Array)
                {
                    errorPatterns.AddRange(ep.EnumerateArray().Select(e => e.GetString()!).Where(e => !string.IsNullOrWhiteSpace(e)));
                }

                if (errorPatterns.Count > 0)
                {
                    var patternsStr = string.Join("; ", errorPatterns.Take(3));
                    chain.LogsEvidence = new EvidenceItem
                    {
                        Source = "elasticsearch",
                        Detail = $"Error patterns identified: {patternsStr}",
                        Confidence = 0.92
                    };
                    contributingFactors.Add($"Application logs show errors: {patternsStr}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse logsJson in correlate_evidence");
            }
        }

        // 2. Process Metrics Evidence
        if (!string.IsNullOrWhiteSpace(metricsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(metricsJson);
                var root = doc.RootElement;

                var spikes = new List<string>();
                if (root.TryGetProperty("anomalies", out var anomArr) && anomArr.ValueKind == JsonValueKind.Array)
                {
                    spikes.AddRange(anomArr.EnumerateArray().Select(a => a.GetString()!).Where(a => !string.IsNullOrWhiteSpace(a)));
                }

                if (root.TryGetProperty("resource_saturation", out var satObj) && satObj.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in satObj.EnumerateObject())
                    {
                        if (prop.Value.TryGetProperty("current", out var curVal))
                        {
                            var cur = curVal.GetDouble();
                            if (prop.Name.Contains("connection") && cur >= 400)
                            {
                                spikes.Add($"Database connection pool near capacity ({cur})");
                            }
                            else if (prop.Name.Contains("cpu") && cur >= 85)
                            {
                                spikes.Add($"CPU saturation at {cur}%");
                            }
                            else if (prop.Name.Contains("memory") && cur >= 85)
                            {
                                spikes.Add($"Memory pressure at {cur}%");
                            }
                        }
                    }
                }

                if (spikes.Count > 0)
                {
                    var spikesStr = string.Join("; ", spikes.Distinct().Take(3));
                    chain.MetricsEvidence = new EvidenceItem
                    {
                        Source = "prometheus",
                        Detail = $"Resource anomalies: {spikesStr}",
                        Confidence = 0.94
                    };
                    contributingFactors.Add($"Infrastructure metrics indicate: {spikesStr}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse metricsJson in correlate_evidence");
            }
        }

        // 3. Process Runbook & History Evidence
        var similarPast = new List<SimilarIncidentReference>();
        if (!string.IsNullOrWhiteSpace(runbooksJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(runbooksJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("similar_past_incidents", out var pastArr) && pastArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in pastArr.EnumerateArray())
                    {
                        var pastItem = JsonSerializer.Deserialize<SimilarIncidentReference>(elem.GetRawText(), JsonOptions);
                        if (pastItem != null)
                        {
                            similarPast.Add(pastItem);
                        }
                    }
                }

                if (root.TryGetProperty("retrieved_runbooks", out var rbArr) && rbArr.ValueKind == JsonValueKind.Array && rbArr.GetArrayLength() > 0)
                {
                    var firstRb = rbArr[0];
                    var title = firstRb.TryGetProperty("title", out var t) ? t.GetString() : "Runbook";
                    var score = firstRb.TryGetProperty("similarity_score", out var sc) ? sc.GetDouble() : 0.90;

                    chain.RunbookEvidence = new EvidenceItem
                    {
                        Source = "runbook_knowledge_base",
                        Detail = $"Matching operational runbook: '{title}'",
                        Confidence = score
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse runbooksJson in correlate_evidence");
            }
        }

        return JsonSerializer.Serialize(new
        {
            evidence_chain = chain,
            contributing_factors = contributingFactors,
            similar_incidents = similarPast
        });
    }

    [KernelFunction("classify_root_cause")]
    [Description("Classifies the incident root cause into an official taxonomy category (DEPLOYMENT, RESOURCE, DATABASE, EXTERNAL, CONFIG, CASCADE, HARDWARE)")]
    public string ClassifyRootCause(
        [Description("Evidence chain JSON string")] string evidenceChainJson)
    {
        _logger.LogInformation("Tool classify_root_cause invoked");

        string category = RootCauseCategories.Unknown;
        string rootCauseDescription = "Root cause under active diagnostic investigation";

        try
        {
            using var doc = JsonDocument.Parse(evidenceChainJson);
            var root = doc.RootElement;

            bool hasDeployment = false;
            string deploymentDetail = string.Empty;
            if (root.TryGetProperty("evidence_chain", out var chain) || root.ValueKind == JsonValueKind.Object)
            {
                var target = root.TryGetProperty("evidence_chain", out var ec) ? ec : root;

                if (target.TryGetProperty("deployment_evidence", out var dep) && dep.ValueKind == JsonValueKind.Object)
                {
                    hasDeployment = true;
                    deploymentDetail = dep.TryGetProperty("detail", out var d) ? d.GetString() ?? string.Empty : string.Empty;
                }

                string logsDetail = string.Empty;
                if (target.TryGetProperty("logs_evidence", out var log) && log.ValueKind == JsonValueKind.Object)
                {
                    logsDetail = log.TryGetProperty("detail", out var ld) ? ld.GetString() ?? string.Empty : string.Empty;
                }

                string metricsDetail = string.Empty;
                if (target.TryGetProperty("metrics_evidence", out var met) && met.ValueKind == JsonValueKind.Object)
                {
                    metricsDetail = met.TryGetProperty("detail", out var md) ? md.GetString() ?? string.Empty : string.Empty;
                }

                var combinedText = $"{deploymentDetail} {logsDetail} {metricsDetail}".ToLowerInvariant();

                if (hasDeployment && (combinedText.Contains("database") || combinedText.Contains("pool") || combinedText.Contains("timeout")))
                {
                    category = RootCauseCategories.Deployment;
                    rootCauseDescription = $"Database connection pool exhaustion and timeout errors caused by unoptimized query introduced in recent deployment ({deploymentDetail})";
                }
                else if (hasDeployment)
                {
                    category = RootCauseCategories.Deployment;
                    rootCauseDescription = $"Software regression introduced by recent deployment: {deploymentDetail}";
                }
                else if (combinedText.Contains("connection pool") || combinedText.Contains("database") || combinedText.Contains("deadlock") || combinedText.Contains("postgres"))
                {
                    category = RootCauseCategories.Database;
                    rootCauseDescription = "Database connection pool exhaustion or query lock contention";
                }
                else if (combinedText.Contains("cpu") || combinedText.Contains("memory") || combinedText.Contains("oom") || combinedText.Contains("disk"))
                {
                    category = RootCauseCategories.Resource;
                    rootCauseDescription = "Resource saturation: High compute/memory pressure exceeding capacity thresholds";
                }
                else if (combinedText.Contains("external") || combinedText.Contains("gateway") || combinedText.Contains("upstream") || combinedText.Contains("dns"))
                {
                    category = RootCauseCategories.External;
                    rootCauseDescription = "Downstream third-party API or external dependency failure";
                }
                else if (combinedText.Contains("config") || combinedText.Contains("missing key") || combinedText.Contains("secret"))
                {
                    category = RootCauseCategories.Config;
                    rootCauseDescription = "Service configuration mismatch or missing environment variable";
                }
                else
                {
                    category = RootCauseCategories.Unknown;
                    rootCauseDescription = "Undetermined incident pattern; manual inspection required";
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to classify root cause from evidence chain");
        }

        return JsonSerializer.Serialize(new
        {
            root_cause_category = category,
            root_cause = rootCauseDescription
        });
    }

    [KernelFunction("calculate_confidence")]
    [Description("Calculates composite confidence score (0.0 to 1.0) based on corroborating evidence signals")]
    public string CalculateConfidence(
        [Description("Evidence chain JSON string")] string evidenceChainJson)
    {
        _logger.LogInformation("Tool calculate_confidence invoked");

        double totalWeight = 0;
        double weightedScore = 0;

        try
        {
            using var doc = JsonDocument.Parse(evidenceChainJson);
            var root = doc.RootElement;
            var target = root.TryGetProperty("evidence_chain", out var ec) ? ec : root;

            if (target.TryGetProperty("deployment_evidence", out var dep) && dep.ValueKind == JsonValueKind.Object)
            {
                var c = dep.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.90;
                weightedScore += c * 0.35;
                totalWeight += 0.35;
            }

            if (target.TryGetProperty("logs_evidence", out var log) && log.ValueKind == JsonValueKind.Object)
            {
                var c = log.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.85;
                weightedScore += c * 0.25;
                totalWeight += 0.25;
            }

            if (target.TryGetProperty("metrics_evidence", out var met) && met.ValueKind == JsonValueKind.Object)
            {
                var c = met.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.85;
                weightedScore += c * 0.25;
                totalWeight += 0.25;
            }

            if (target.TryGetProperty("runbook_evidence", out var rb) && rb.ValueKind == JsonValueKind.Object)
            {
                var c = rb.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 0.80;
                weightedScore += c * 0.15;
                totalWeight += 0.15;
            }
        }
        catch { }

        double finalScore = totalWeight > 0 ? (weightedScore / totalWeight) : 0.70;
        // Boost slightly if multiple cross-domain sources corroborating
        if (totalWeight >= 0.70)
        {
            finalScore = Math.Min(0.98, finalScore + 0.04);
        }

        return JsonSerializer.Serialize(new
        {
            confidence_score = Math.Round(finalScore, 2)
        });
    }
}
