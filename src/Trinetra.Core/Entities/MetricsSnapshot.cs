namespace Trinetra.Core.Entities;

public class MetricsSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }

    // Query Details
    public string Source { get; set; } = string.Empty; // 'prometheus', 'datadog', 'newrelic'
    public string? QueryExecuted { get; set; }
    public DateTime TimeRangeStart { get; set; } = DateTime.UtcNow;
    public DateTime TimeRangeEnd { get; set; } = DateTime.UtcNow;

    // Raw Data
    public string MetricsData { get; set; } = "{}"; // JSONB
    public List<string> MetricNames { get; set; } = new();

    // Analysis
    public List<string> AnomaliesDetected { get; set; } = new();
    public string? ResourceSaturation { get; set; } // JSONB
    public string? BaselineDeviations { get; set; } // JSONB

    // Metadata
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnalyzedAt { get; set; }

    // Navigation
    public Incident Incident { get; set; } = null!;
}
