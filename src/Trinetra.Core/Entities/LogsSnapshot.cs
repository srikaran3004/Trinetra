namespace Trinetra.Core.Entities;

public class LogsSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }

    // Query Details
    public string Source { get; set; } = string.Empty; // 'elasticsearch', 'splunk', 'datadog'
    public string? QueryExecuted { get; set; }
    public DateTime? TimeRangeStart { get; set; }
    public DateTime? TimeRangeEnd { get; set; }

    // Raw Data
    public string LogData { get; set; } = "[]"; // JSONB
    public int LogCount { get; set; } = 0;

    // Analysis
    public List<string> ErrorPatterns { get; set; } = new();
    public List<string> StackTraces { get; set; } = new();
    public List<string> AnomaliesDetected { get; set; } = new();
    public string? SeverityDistribution { get; set; } // JSONB

    // Metadata
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnalyzedAt { get; set; }

    // Navigation
    public Incident Incident { get; set; } = null!;
}
