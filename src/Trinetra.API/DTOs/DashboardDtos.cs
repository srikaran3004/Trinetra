using System.Text.Json.Serialization;

namespace Trinetra.API.DTOs;

public record DashboardSummaryDto
{
    [JsonPropertyName("active_incidents")]
    public int ActiveIncidents { get; init; }

    [JsonPropertyName("critical_incidents")]
    public int CriticalIncidents { get; init; }

    [JsonPropertyName("avg_mttr_minutes")]
    public decimal AvgMttrMinutes { get; init; }

    [JsonPropertyName("auto_resolution_rate")]
    public decimal AutoResolutionRate { get; init; }

    [JsonPropertyName("incidents_this_week")]
    public int IncidentsThisWeek { get; init; }

    [JsonPropertyName("most_common_root_cause")]
    public string MostCommonRootCause { get; init; } = "none";

    [JsonPropertyName("recent_incidents")]
    public IEnumerable<IncidentListItemDto> RecentIncidents { get; init; } = Enumerable.Empty<IncidentListItemDto>();
}

public record DashboardMetricsDto
{
    [JsonPropertyName("incident_trends")]
    public TrendMetricDto IncidentTrends { get; init; } = new();

    [JsonPropertyName("severity_distribution")]
    public Dictionary<string, int> SeverityDistribution { get; init; } = new();

    [JsonPropertyName("resolution_time_distribution")]
    public Dictionary<string, int> ResolutionTimeDistribution { get; init; } = new();
}

public record TrendMetricDto
{
    [JsonPropertyName("labels")]
    public List<string> Labels { get; init; } = new();

    [JsonPropertyName("data")]
    public List<int> Data { get; init; } = new();
}
