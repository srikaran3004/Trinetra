using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Trinetra.Core.Models.Runbooks;

public class RemediationStepItem
{
    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("expected_result")]
    public string? ExpectedResult { get; set; }

    [JsonPropertyName("risk")]
    public string? Risk { get; set; } // low, medium, high
}

public class RetrievedRunbookItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("similarity_score")]
    public double SimilarityScore { get; set; }

    [JsonPropertyName("applicable_services")]
    public List<string> ApplicableServices { get; set; } = new();

    [JsonPropertyName("estimated_time_minutes")]
    public int EstimatedTimeMinutes { get; set; } = 10;

    [JsonPropertyName("remediation_steps")]
    public List<RemediationStepItem> RemediationSteps { get; set; } = new();

    [JsonPropertyName("risks")]
    public List<string> Risks { get; set; } = new();

    [JsonPropertyName("prerequisites")]
    public List<string> Prerequisites { get; set; } = new();
}

public class SimilarPastIncidentItem
{
    [JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("root_cause")]
    public string RootCause { get; set; } = string.Empty;

    [JsonPropertyName("resolution_applied")]
    public string ResolutionApplied { get; set; } = string.Empty;

    [JsonPropertyName("resolution_time_minutes")]
    public int ResolutionTimeMinutes { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;

    [JsonPropertyName("similarity_score")]
    public double SimilarityScore { get; set; }
}

public class RunbookRetrievalReport
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; set; }

    [JsonPropertyName("retrieved_runbooks")]
    public List<RetrievedRunbookItem> RetrievedRunbooks { get; set; } = new();

    [JsonPropertyName("similar_past_incidents")]
    public List<SimilarPastIncidentItem> SimilarPastIncidents { get; set; } = new();

    [JsonPropertyName("knowledge_gaps")]
    public List<string> KnowledgeGaps { get; set; } = new();

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; } = 0.95m;
}
