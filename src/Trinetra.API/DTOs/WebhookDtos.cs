using System.Text.Json.Serialization;

namespace Trinetra.API.DTOs;

public record PrometheusAlertPayload
{
    [JsonPropertyName("alerts")]
    public List<PrometheusAlertItem>? Alerts { get; init; }
}

public record PrometheusAlertItem
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("labels")]
    public Dictionary<string, string>? Labels { get; init; }

    [JsonPropertyName("annotations")]
    public Dictionary<string, string>? Annotations { get; init; }

    [JsonPropertyName("startsAt")]
    public DateTime? StartsAt { get; init; }

    [JsonPropertyName("endsAt")]
    public DateTime? EndsAt { get; init; }
}

public record DatadogAlertPayload
{
    [JsonPropertyName("alert")]
    public DatadogAlertItem? Alert { get; init; }
}

public record DatadogAlertItem
{
    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("metric")]
    public string? Metric { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("metric_query")]
    public string? MetricQuery { get; init; }

    [JsonPropertyName("threshold_value")]
    public decimal? ThresholdValue { get; init; }

    [JsonPropertyName("current_value")]
    public decimal? CurrentValue { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; init; }
}

public record WebhookAlertResponse
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "triggered";

    [JsonPropertyName("investigation_starting")]
    public bool InvestigationStarting { get; init; } = true;
}
