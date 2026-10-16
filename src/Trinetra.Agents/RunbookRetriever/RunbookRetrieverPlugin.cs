using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Runbooks;

namespace Trinetra.Agents.RunbookRetriever;

public class RunbookRetrieverPlugin
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IRunbookRepository _runbookRepository;
    private readonly ILogger<RunbookRetrieverPlugin> _logger;

    public RunbookRetrieverPlugin(
        IEmbeddingService embeddingService,
        IRunbookRepository runbookRepository,
        ILogger<RunbookRetrieverPlugin> logger)
    {
        _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
        _runbookRepository = runbookRepository ?? throw new ArgumentNullException(nameof(runbookRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("search_runbooks_by_embedding")]
    [Description("Performs semantic similarity search on runbooks using pgvector cosine distance")]
    public async Task<string> SearchRunbooksByEmbeddingAsync(
        [Description("Incident summary or error description to embed and search")] string incidentSummary,
        [Description("Maximum number of results to return (default 5)")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool search_runbooks_by_embedding invoked with topK {TopK}", topK);

        var embedding = await _embeddingService.GenerateEmbeddingAsync(incidentSummary, cancellationToken)
            .ConfigureAwait(false);

        var results = await _runbookRepository.SearchByEmbeddingAsync(embedding, topK, 0.5, cancellationToken)
            .ConfigureAwait(false);

        var runbooksList = new List<object>();
        var similarityScores = new List<double>();

        foreach (var (rb, sim) in results)
        {
            similarityScores.Add(sim);
            runbooksList.Add(new
            {
                id = rb.Id,
                title = rb.Title,
                description = rb.Description,
                applicable_services = rb.ApplicableServices,
                estimated_time_minutes = rb.EstimatedTimeMinutes ?? 10,
                similarity_score = sim,
                risks = rb.RisksAndWarnings,
                prerequisites = rb.Prerequisites
            });
        }

        return JsonSerializer.Serialize(new
        {
            runbooks = runbooksList,
            similarity_scores = similarityScores
        });
    }

    [KernelFunction("search_runbooks_by_keyword")]
    [Description("Searches runbooks using keyword and pattern matching across titles, errors, and tags")]
    public async Task<string> SearchRunbooksByKeywordAsync(
        [Description("JSON array of keywords or comma-separated keyword string")] string keywords,
        [Description("Maximum number of results to return (default 5)")] int topK = 5,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool search_runbooks_by_keyword invoked for '{Keywords}', topK {TopK}", keywords, topK);

        var kwList = ParseKeywords(keywords);
        var results = await _runbookRepository.SearchByKeywordsAsync(kwList, topK, cancellationToken)
            .ConfigureAwait(false);

        var runbooksList = new List<object>();
        var matchScores = new List<double>();

        foreach (var (rb, score) in results)
        {
            matchScores.Add(score);
            runbooksList.Add(new
            {
                id = rb.Id,
                title = rb.Title,
                description = rb.Description,
                applicable_services = rb.ApplicableServices,
                estimated_time_minutes = rb.EstimatedTimeMinutes ?? 10,
                match_score = score,
                risks = rb.RisksAndWarnings
            });
        }

        return JsonSerializer.Serialize(new
        {
            runbooks = runbooksList,
            match_scores = matchScores
        });
    }

    [KernelFunction("search_incident_history")]
    [Description("Finds similar past historical incidents and their applied resolutions")]
    public async Task<string> SearchIncidentHistoryAsync(
        [Description("Incident summary or root cause description to search for")] string incidentSummary,
        [Description("Maximum number of past incidents to return (default 3)")] int topK = 3,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool search_incident_history invoked for topK {TopK}", topK);

        float[]? embedding = null;
        try
        {
            embedding = await _embeddingService.GenerateEmbeddingAsync(incidentSummary, cancellationToken).ConfigureAwait(false);
        }
        catch { }

        var results = await _runbookRepository.SearchIncidentHistoryAsync(embedding, incidentSummary, topK, cancellationToken)
            .ConfigureAwait(false);
        results ??= new();

        var pastIncidents = new List<object>();
        var solutionsApplied = new List<string>();

        foreach (var (h, sim) in results)
        {
            solutionsApplied.Add(h.RemediationAction ?? "No specific action recorded");
            pastIncidents.Add(new
            {
                date = h.OccurredAt?.ToString("yyyy-MM-dd") ?? h.CreatedAt.ToString("yyyy-MM-dd"),
                title = h.Title,
                root_cause = h.RootCause,
                resolution_applied = h.RemediationAction,
                resolution_time_minutes = h.ResolutionTimeMinutes ?? h.RemediationTimeMinutes ?? 10,
                success = true,
                similarity_score = sim
            });
        }

        return JsonSerializer.Serialize(new
        {
            past_incidents = pastIncidents,
            solutions_applied = solutionsApplied
        });
    }

    [KernelFunction("extract_remediation_steps")]
    [Description("Extracts structured remediation steps, estimated time, and risks from a specific runbook")]
    public async Task<string> ExtractRemediationStepsAsync(
        [Description("Runbook GUID identifier")] string runbookId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool extract_remediation_steps invoked for runbook {RunbookId}", runbookId);

        if (!Guid.TryParse(runbookId, out var id))
        {
            return JsonSerializer.Serialize(new
            {
                steps = Array.Empty<RemediationStepItem>(),
                time_estimate = 10,
                risks = Array.Empty<string>()
            });
        }

        var runbook = await _runbookRepository.GetRunbookByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (runbook == null)
        {
            return JsonSerializer.Serialize(new
            {
                steps = Array.Empty<RemediationStepItem>(),
                time_estimate = 10,
                risks = Array.Empty<string>()
            });
        }

        var steps = new List<RemediationStepItem>();

        // 1. Try parsing from structured RemediationSteps JSONB
        if (!string.IsNullOrWhiteSpace(runbook.RemediationSteps))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<RemediationStepItem>>(runbook.RemediationSteps);
                if (parsed != null && parsed.Count > 0)
                {
                    steps = parsed;
                }
            }
            catch { }
        }

        // 2. If no structured steps found, parse from Markdown Content
        if (steps.Count == 0 && !string.IsNullOrWhiteSpace(runbook.Content))
        {
            steps = ParseStepsFromMarkdown(runbook.Content);
        }

        return JsonSerializer.Serialize(new
        {
            steps = steps,
            time_estimate = runbook.EstimatedTimeMinutes ?? 10,
            risks = runbook.RisksAndWarnings
        });
    }

    private static List<RemediationStepItem> ParseStepsFromMarkdown(string markdown)
    {
        var steps = new List<RemediationStepItem>();
        var stepRegex = new Regex(@"###?\s*Step\s*(\d+)[:\.]?\s*([^\r\n]+)", RegexOptions.IgnoreCase);
        var matches = stepRegex.Matches(markdown);

        int index = 1;
        foreach (Match match in matches)
        {
            var stepNum = int.TryParse(match.Groups[1].Value, out var n) ? n : index;
            var action = match.Groups[2].Value.Trim();

            // Check if there is a bash / command block right after
            var stepContentStart = match.Index + match.Length;
            var nextStepMatch = match.NextMatch();
            var stepContentEnd = nextStepMatch.Success ? nextStepMatch.Index : markdown.Length;
            var stepText = markdown.Substring(stepContentStart, stepContentEnd - stepContentStart);

            var codeBlockRegex = new Regex(@"```(?:bash|sh|sql)?\s*([\s\S]*?)\s*```");
            var codeMatch = codeBlockRegex.Match(stepText);
            var command = codeMatch.Success ? codeMatch.Groups[1].Value.Trim() : string.Empty;

            var fullStepText = $"{action} {stepText}";
            var risk = fullStepText.Contains("high", StringComparison.OrdinalIgnoreCase) ? "high"
                     : fullStepText.Contains("medium", StringComparison.OrdinalIgnoreCase) ? "medium"
                     : "low";

            steps.Add(new RemediationStepItem
            {
                Step = stepNum,
                Action = action,
                Command = command,
                Risk = risk
            });
            index++;
        }

        return steps;
    }

    private static List<string> ParseKeywords(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();

        var trimmed = input.Trim();
        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed);
                if (parsed != null) return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            catch { }
        }

        return trimmed.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
