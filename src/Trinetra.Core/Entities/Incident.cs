namespace Trinetra.Core.Entities;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string AlertId { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // 'prometheus', 'datadog', 'pagerduty', etc.

    // Basic Info
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Classification
    public string Severity { get; set; } = "medium"; // 'critical', 'high', 'medium', 'low'
    public string Status { get; set; } = "triggered"; // 'triggered', 'investigating', 'analysis_complete', etc.

    // Affected Resources
    public List<string> AffectedServices { get; set; } = new();
    public List<string> AffectedHosts { get; set; } = new();
    public List<string> AffectedComponents { get; set; } = new();

    // Analysis Results
    public string? RootCause { get; set; }
    public string? RootCauseCategory { get; set; }
    public decimal? RootCauseConfidence { get; set; }

    // Remediation
    public string? ProposedRemediation { get; set; }
    public string? RemediationRiskLevel { get; set; }
    public int? RemediationEstimatedTimeMinutes { get; set; }

    // Timeline
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedInvestigatingAt { get; set; }
    public DateTime? AnalysisCompletedAt { get; set; }
    public DateTime? RemediationStartedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? EscalatedAt { get; set; }

    // Assignment
    public Guid? AssignedTo { get; set; }
    public string? AssignedTeam { get; set; }

    // Metadata
    public string? CreatedByAgent { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? AssignedUser { get; set; }
    public ICollection<IncidentEvent> Events { get; set; } = new List<IncidentEvent>();
    public ICollection<LogsSnapshot> LogsSnapshots { get; set; } = new List<LogsSnapshot>();
    public ICollection<MetricsSnapshot> MetricsSnapshots { get; set; } = new List<MetricsSnapshot>();
}
