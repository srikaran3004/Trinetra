using System;
using System.Collections.Generic;

namespace Trinetra.Core.Models.Metrics;

public class MetricSample
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Value { get; set; }
}

public class MetricSeries
{
    public string MetricName { get; set; } = string.Empty;
    public Dictionary<string, string> Labels { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<MetricSample> Samples { get; set; } = new();
}

public class MetricQueryResult
{
    public string Source { get; set; } = "prometheus";
    public Dictionary<string, List<MetricSample>> Metrics { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime? TimeRangeStart { get; set; }
    public DateTime? TimeRangeEnd { get; set; }
}

public class MetricBaselineResult
{
    public string Service { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public double P50 { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
    public double Avg { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public int LookbackDays { get; set; } = 7;
}

public class ResourceSaturationItem
{
    public double Current { get; set; }
    public double BaselineP95 { get; set; }
    public double SpikePercent { get; set; }
    public double? UtilizationPercent { get; set; }
    public double? MaxPool { get; set; }
    public double? Available { get; set; }
}

public class ResourceSaturationResult
{
    [System.Text.Json.Serialization.JsonPropertyName("saturated_resources")]
    public List<string> SaturatedResources { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("spikes")]
    public List<string> Spikes { get; set; } = new();

    [System.Text.Json.Serialization.JsonPropertyName("resource_saturation")]
    public Dictionary<string, ResourceSaturationItem> ResourceSaturation { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class CorrelationAnalysisResult
{
    [System.Text.Json.Serialization.JsonPropertyName("anomaly_start")]
    public DateTime? AnomalyStart { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("incident_start")]
    public DateTime? IncidentStart { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("offset_seconds")]
    public int OffsetSeconds { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("metrics_led_incident")]
    public bool MetricsLedIncident { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("correlated_metrics")]
    public List<string> CorrelatedMetrics { get; set; } = new();
}

public class MetricsAnalysisReport
{
    public Guid IncidentId { get; set; }
    public string MetricsSource { get; set; } = "prometheus";
    public DateTime TimeRangeStart { get; set; }
    public DateTime TimeRangeEnd { get; set; }
    public Dictionary<string, ResourceSaturationItem> ResourceSaturation { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Anomalies { get; set; } = new();
    public CorrelationAnalysisResult? CorrelationAnalysis { get; set; }
    public string Summary { get; set; } = string.Empty;
    public decimal Confidence { get; set; } = 0.90m;
    public Guid? SnapshotId { get; set; }
}
