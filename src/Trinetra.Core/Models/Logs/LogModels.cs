using System;
using System.Collections.Generic;

namespace Trinetra.Core.Models.Logs;

public class LogEntry
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Level { get; set; } = "Information";
    public string Message { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? Host { get; set; }
    public string? TraceId { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = new();
}

public class LogQueryResult
{
    public IReadOnlyList<LogEntry> Logs { get; set; } = Array.Empty<LogEntry>();
    public int TotalCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTime? TimeRangeStart { get; set; }
    public DateTime? TimeRangeEnd { get; set; }
}

public class StackTraceExtractionResult
{
    public List<string> StackTraces { get; set; } = new();
    public List<string> Exceptions { get; set; } = new();
    public List<string> Languages { get; set; } = new();
}

public class LogAnomalyDetectionResult
{
    public List<string> Anomalies { get; set; } = new();
    public string Severity { get; set; } = "low"; // low, medium, high, critical
    public List<string> ErrorPatterns { get; set; } = new();
    public Dictionary<string, int> SeverityDistribution { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class DeploymentCorrelationResult
{
    public List<DeploymentInfo> RecentDeployments { get; set; } = new();
    public List<string> ChangedFiles { get; set; } = new();
}

public class DeploymentInfo
{
    public string Service { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTime DeployedAt { get; set; }
    public List<string> ChangedFiles { get; set; } = new();
}

public class LogAnalysisReport
{
    public Guid IncidentId { get; set; }
    public string Source { get; set; } = "elasticsearch";
    public int LogsRetrieved { get; set; }
    public int ErrorCount { get; set; }
    public List<string> ErrorPatterns { get; set; } = new();
    public List<string> StackTraces { get; set; } = new();
    public List<string> Anomalies { get; set; } = new();
    public List<DeploymentInfo> RelatedDeployments { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public decimal Confidence { get; set; } = 0.85m;
    public Guid? SnapshotId { get; set; }
}

public static class TimeRangeParser
{
    public static TimeSpan Parse(string? timeRange)
    {
        if (string.IsNullOrWhiteSpace(timeRange))
            return TimeSpan.FromMinutes(15);

        var match = System.Text.RegularExpressions.Regex.Match(timeRange.Trim(), @"^(\d+)\s*([smhdw])$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
            return TimeSpan.FromMinutes(15);

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLower();

        return unit switch
        {
            "s" => TimeSpan.FromSeconds(value),
            "m" => TimeSpan.FromMinutes(value),
            "h" => TimeSpan.FromHours(value),
            "d" => TimeSpan.FromDays(value),
            "w" => TimeSpan.FromDays(value * 7),
            _ => TimeSpan.FromMinutes(15)
        };
    }
}

