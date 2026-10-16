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
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Runbooks;

namespace Trinetra.Agents.RunbookRetriever;

public class RunbookRetrieverAgent : AgentBase
{
    public const string AgentIdentifier = "runbook_retriever";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RunbookRetrieverPlugin _plugin;

    public RunbookRetrieverAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<RunbookRetrieverAgent> logger,
        RunbookRetrieverPlugin plugin,
        IAuditEventWriter? auditWriter = null)
        : base(AgentIdentifier, configuration, kernel, logger, auditWriter)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));

        // Register tools into Semantic Kernel
        RegisterPlugin(_plugin, "RunbookRetriever");
    }

    public async Task<RunbookRetrievalReport> RetrieveRunbooksAsync(
        Guid incidentId,
        string incidentSummary,
        List<string>? errorPatterns = null,
        string? serviceName = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartAgentActivity("retrieve_runbooks");
        if (activity != null)
        {
            activity.SetTag("trinetra.incident.id", incidentId.ToString());
            activity.SetTag("trinetra.service", serviceName ?? "unknown");
        }

        var sw = Stopwatch.StartNew();

        // 1. Audit Trail Ground Rule: Record start of retrieval
        await RecordAuditEventAsync(
            incidentId,
            "runbook_retrieval_started",
            $"Runbook Retriever started search for operational runbooks matching '{incidentSummary}'",
            toolCall: "search_runbooks_by_embedding",
            toolParams: new { incidentSummary, errorPatterns, serviceName },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            var retrievedRunbooks = new List<RetrievedRunbookItem>();
            var knowledgeGaps = new List<string>();

            // 2. Search runbooks using pgvector semantic embedding
            var embeddingJson = await _plugin.SearchRunbooksByEmbeddingAsync(incidentSummary, topK: 5, cancellationToken)
                .ConfigureAwait(false);

            using (var doc = JsonDocument.Parse(embeddingJson))
            {
                if (doc.RootElement.TryGetProperty("runbooks", out var rbArr) && rbArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in rbArr.EnumerateArray())
                    {
                        var item = JsonSerializer.Deserialize<RetrievedRunbookItem>(elem.GetRawText(), JsonOptions);
                        if (item != null)
                        {
                            retrievedRunbooks.Add(item);
                        }
                    }
                }
            }

            // 3. Fallback or augment with keyword search if needed
            if (retrievedRunbooks.Count == 0 || retrievedRunbooks.All(r => r.SimilarityScore < 0.70))
            {
                var keywords = new List<string>();
                if (!string.IsNullOrWhiteSpace(serviceName)) keywords.Add(serviceName);
                if (errorPatterns != null) keywords.AddRange(errorPatterns.Take(3));
                keywords.AddRange(incidentSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(4));

                var kwJson = await _plugin.SearchRunbooksByKeywordAsync(string.Join(",", keywords), topK: 5, cancellationToken)
                    .ConfigureAwait(false);

                using var doc = JsonDocument.Parse(kwJson);
                if (doc.RootElement.TryGetProperty("runbooks", out var rbArr) && rbArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in rbArr.EnumerateArray())
                    {
                        var item = JsonSerializer.Deserialize<RetrievedRunbookItem>(elem.GetRawText(), JsonOptions);
                        if (item != null && !retrievedRunbooks.Any(r => r.Id == item.Id))
                        {
                            if (elem.TryGetProperty("match_score", out var ms))
                            {
                                item.SimilarityScore = ms.GetDouble();
                            }
                            retrievedRunbooks.Add(item);
                        }
                    }
                }
            }

            // 4. Search incident history for past similar incidents and solutions
            var pastIncidents = new List<SimilarPastIncidentItem>();
            var historyJson = await _plugin.SearchIncidentHistoryAsync(incidentSummary, topK: 3, cancellationToken)
                .ConfigureAwait(false);

            using (var doc = JsonDocument.Parse(historyJson))
            {
                if (doc.RootElement.TryGetProperty("past_incidents", out var piArr) && piArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var elem in piArr.EnumerateArray())
                    {
                        var past = JsonSerializer.Deserialize<SimilarPastIncidentItem>(elem.GetRawText(), JsonOptions);
                        if (past != null)
                        {
                            pastIncidents.Add(past);
                        }
                    }
                }
            }

            // 5. Extract remediation steps for top runbooks
            foreach (var runbook in retrievedRunbooks.Take(2))
            {
                if (runbook.RemediationSteps.Count == 0)
                {
                    var stepsJson = await _plugin.ExtractRemediationStepsAsync(runbook.Id.ToString(), cancellationToken)
                        .ConfigureAwait(false);

                    using var doc = JsonDocument.Parse(stepsJson);
                    if (doc.RootElement.TryGetProperty("steps", out var stepsArr) && stepsArr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var stepElem in stepsArr.EnumerateArray())
                        {
                            var stepItem = JsonSerializer.Deserialize<RemediationStepItem>(stepElem.GetRawText(), JsonOptions);
                            if (stepItem != null)
                            {
                                runbook.RemediationSteps.Add(stepItem);
                            }
                        }
                    }
                }
            }

            // 6. Identify Knowledge Gaps
            if (retrievedRunbooks.Count == 0)
            {
                knowledgeGaps.Add($"No runbooks found matching incident pattern for service '{serviceName ?? "general"}'");
            }
            else if (retrievedRunbooks.All(r => r.SimilarityScore < 0.75))
            {
                knowledgeGaps.Add("Runbook match confidence is moderate (< 75%); manual verification recommended before automated execution.");
            }

            // 7. Formulate Summary and Confidence Score
            var bestScore = retrievedRunbooks.Count > 0 ? retrievedRunbooks.Max(r => r.SimilarityScore) : 0.0;
            var confidence = bestScore >= 0.85 ? 0.96m : bestScore >= 0.70 ? 0.88m : 0.65m;

            string summary;
            if (retrievedRunbooks.Count > 0)
            {
                var topRb = retrievedRunbooks.First();
                var pastStr = pastIncidents.Count > 0 ? $" and {pastIncidents.Count} similar historical incident(s)" : string.Empty;
                summary = $"Found relevant runbook '{topRb.Title}' (score: {topRb.SimilarityScore:P0}){pastStr} with {topRb.RemediationSteps.Count} remediation steps.";
            }
            else
            {
                summary = "No matching runbooks found for incident symptoms; flagged as knowledge gap.";
            }

            var report = new RunbookRetrievalReport
            {
                IncidentId = incidentId,
                RetrievedRunbooks = retrievedRunbooks,
                SimilarPastIncidents = pastIncidents,
                KnowledgeGaps = knowledgeGaps,
                Summary = summary,
                Confidence = confidence
            };

            sw.Stop();

            // 8. Audit Trail Ground Rule: Record completion
            await RecordAuditEventAsync(
                incidentId,
                "runbook_retrieval_completed",
                $"Completed runbook retrieval: {summary}",
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
            Logger.LogError(ex, "Runbook Retriever failed for incident {IncidentId}", incidentId);

            await RecordAuditEventAsync(
                incidentId,
                "runbook_retrieval_failed",
                $"Runbook retrieval failed: {ex.Message}",
                executionStatus: "failed",
                executionError: ex.ToString(),
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
