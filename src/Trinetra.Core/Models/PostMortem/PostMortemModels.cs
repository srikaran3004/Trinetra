using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Trinetra.Core.Models.PostMortem;

public class PostMortemTimelineEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("event_type")]
    public string EventType { get; set; } = string.Empty;

    [JsonPropertyName("actor")]
    public string Actor { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("duration_ms")]
    public int? DurationMs { get; set; }
}

public class PostMortemReport
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("triggered_at")]
    public DateTime TriggeredAt { get; set; }

    [JsonPropertyName("resolved_at")]
    public DateTime? ResolvedAt { get; set; }

    [JsonPropertyName("total_duration_minutes")]
    public int? TotalDurationMinutes { get; set; }

    [JsonPropertyName("affected_services")]
    public List<string> AffectedServices { get; set; } = new();

    [JsonPropertyName("root_cause")]
    public string? RootCause { get; set; }

    [JsonPropertyName("root_cause_category")]
    public string? RootCauseCategory { get; set; }

    [JsonPropertyName("root_cause_confidence")]
    public decimal? RootCauseConfidence { get; set; }

    [JsonPropertyName("remediation_applied")]
    public string? RemediationApplied { get; set; }

    [JsonPropertyName("events_count")]
    public int EventsCount { get; set; }

    [JsonPropertyName("timeline")]
    public List<PostMortemTimelineEntry> Timeline { get; set; } = new();

    [JsonPropertyName("lessons_learned")]
    public List<string> LessonsLearned { get; set; } = new();

    [JsonPropertyName("action_items")]
    public List<string> ActionItems { get; set; } = new();

    [JsonPropertyName("markdown_report")]
    public string MarkdownReport { get; set; } = string.Empty;

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
