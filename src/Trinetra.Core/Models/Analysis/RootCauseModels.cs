using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Trinetra.Core.Models.Analysis;

public static class RootCauseCategories
{
    public const string Deployment = "DEPLOYMENT";
    public const string Resource = "RESOURCE";
    public const string Database = "DATABASE";
    public const string External = "EXTERNAL";
    public const string Config = "CONFIG";
    public const string Cascade = "CASCADE";
    public const string Hardware = "HARDWARE";
    public const string Unknown = "UNKNOWN";
}

public class EvidenceItem
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; set; } = string.Empty;

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; } = 0.90;
}

public class EvidenceChain
{
    [JsonPropertyName("deployment_evidence")]
    public EvidenceItem? DeploymentEvidence { get; set; }

    [JsonPropertyName("logs_evidence")]
    public EvidenceItem? LogsEvidence { get; set; }

    [JsonPropertyName("metrics_evidence")]
    public EvidenceItem? MetricsEvidence { get; set; }

    [JsonPropertyName("runbook_evidence")]
    public EvidenceItem? RunbookEvidence { get; set; }
}

public class SimilarIncidentReference
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("resolution")]
    public string Resolution { get; set; } = string.Empty;
}

public class RootCauseReport
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; set; }

    [JsonPropertyName("root_cause")]
    public string RootCause { get; set; } = string.Empty;

    [JsonPropertyName("root_cause_category")]
    public string RootCauseCategory { get; set; } = RootCauseCategories.Unknown;

    [JsonPropertyName("confidence_score")]
    public decimal ConfidenceScore { get; set; } = 0.88m;

    [JsonPropertyName("contributing_factors")]
    public List<string> ContributingFactors { get; set; } = new();

    [JsonPropertyName("evidence_chain")]
    public EvidenceChain EvidenceChain { get; set; } = new();

    [JsonPropertyName("similar_incidents")]
    public List<SimilarIncidentReference> SimilarIncidents { get; set; } = new();

    [JsonPropertyName("severity_assessment")]
    public string? SeverityAssessment { get; set; }

    [JsonPropertyName("affected_users_estimate")]
    public string? AffectedUsersEstimate { get; set; }
}
