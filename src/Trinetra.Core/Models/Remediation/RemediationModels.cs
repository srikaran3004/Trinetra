using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Trinetra.Core.Models.Remediation;

public static class RemediationRiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
}

public class RemediationOption
{
    [JsonPropertyName("option_id")]
    public int OptionId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("implementation_steps")]
    public List<string> ImplementationSteps { get; set; } = new();

    [JsonPropertyName("action_command")]
    public string? ActionCommand { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = RemediationRiskLevels.Medium;

    [JsonPropertyName("risk_factors")]
    public List<string> RiskFactors { get; set; } = new();

    [JsonPropertyName("estimated_time_minutes")]
    public int EstimatedTimeMinutes { get; set; } = 5;

    [JsonPropertyName("requires_approval")]
    public bool RequiresApproval { get; set; } = true;

    [JsonPropertyName("approval_level")]
    public string? ApprovalLevel { get; set; } = "lead";

    [JsonPropertyName("success_probability")]
    public double SuccessProbability { get; set; } = 0.80;

    [JsonPropertyName("recommended")]
    public bool Recommended { get; set; } = false;

    [JsonPropertyName("preflight_checks")]
    public List<string> PreflightChecks { get; set; } = new();

    [JsonPropertyName("postflight_checks")]
    public List<string> PostflightChecks { get; set; } = new();
}

public class RemediationProposalReport
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; set; }

    [JsonPropertyName("root_cause_summary")]
    public string RootCauseSummary { get; set; } = string.Empty;

    [JsonPropertyName("root_cause_category")]
    public string RootCauseCategory { get; set; } = string.Empty;

    [JsonPropertyName("remediation_options")]
    public List<RemediationOption> RemediationOptions { get; set; } = new();

    [JsonPropertyName("recommended_option")]
    public RemediationOption? RecommendedOption { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
