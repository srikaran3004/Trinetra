using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Metrics;

namespace Trinetra.Agents.MetricsQuerier;

public class MetricsQuerierPlugin
{
    private readonly IPrometheusMetricsService _metricsService;
    private readonly ILogger<MetricsQuerierPlugin> _logger;

    public MetricsQuerierPlugin(
        IPrometheusMetricsService metricsService,
        ILogger<MetricsQuerierPlugin> logger)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("query_metrics")]
    [Description("Query metrics platforms (Prometheus) for given metric names, time range, and aggregation")]
    public async Task<string> QueryMetricsAsync(
        [Description("JSON array of metric names or comma-separated metric names")] string metricNames,
        [Description("Time range window, e.g. '5m', '15m', '1h'")] string timeRange = "15m",
        [Description("Aggregation method: avg, max, min, sum")] string aggregation = "avg",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool query_metrics invoked for '{Names}', range '{Range}', agg '{Agg}'",
            metricNames, timeRange, aggregation);

        var names = ParseMetricNames(metricNames);
        var result = await _metricsService.QueryMetricsAsync(names, timeRange, aggregation, cancellationToken)
            .ConfigureAwait(false);

        return JsonSerializer.Serialize(new
        {
            metrics = result.Metrics.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Select(s => new { timestamp = s.Timestamp.ToString("O"), value = s.Value })
            ),
            time_range = new
            {
                start = result.TimeRangeStart?.ToString("O"),
                end = result.TimeRangeEnd?.ToString("O")
            }
        });
    }

    [KernelFunction("get_baseline_metrics")]
    [Description("Retrieves statistical baseline metrics (p50, p95, p99, avg, min, max) for a service and metric")]
    public async Task<string> GetBaselineMetricsAsync(
        [Description("Target service name")] string service,
        [Description("Target metric name (e.g. 'cpu_percent', 'memory_percent')")] string metric,
        [Description("Lookback window in days (default 7)")] int lookbackDays = 7,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool get_baseline_metrics invoked for service '{Service}', metric '{Metric}'", service, metric);

        var baseline = await _metricsService.GetBaselineMetricsAsync(service, metric, lookbackDays, cancellationToken)
            .ConfigureAwait(false);

        return JsonSerializer.Serialize(new
        {
            service = baseline.Service,
            metric = baseline.Metric,
            p50 = baseline.P50,
            p95 = baseline.P95,
            p99 = baseline.P99,
            avg = baseline.Avg,
            min = baseline.Min,
            max = baseline.Max,
            lookback_days = baseline.LookbackDays
        });
    }

    [KernelFunction("detect_saturation")]
    [Description("Identifies resource saturation limits, spikes, and threshold breaches")]
    public async Task<string> DetectSaturationAsync(
        [Description("Target service name")] string service,
        [Description("Saturation threshold percentage (default 90)")] double thresholdPercent = 90.0,
        [Description("Optional pre-queried metrics JSON")] string? metricsJson = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool detect_saturation invoked for service '{Service}', threshold {Threshold}%", service, thresholdPercent);

        var standardResources = new[] { "cpu_percent", "memory_percent", "database_connections", "disk_io_read_mbps" };
        var metricsData = new Dictionary<string, List<MetricSample>>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(metricsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(metricsJson);
                if (doc.RootElement.TryGetProperty("metrics", out var mElement))
                {
                    foreach (var prop in mElement.EnumerateObject())
                    {
                        var samples = new List<MetricSample>();
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            if (item.TryGetProperty("value", out var v) && item.TryGetProperty("timestamp", out var t))
                            {
                                double val = 0;
                                if (v.ValueKind == JsonValueKind.Number)
                                {
                                    val = v.GetDouble();
                                }
                                else if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVal))
                                {
                                    val = parsedVal;
                                }

                                var timeStr = t.GetString();
                                if (!string.IsNullOrWhiteSpace(timeStr) && DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedTime))
                                {
                                    samples.Add(new MetricSample
                                    {
                                        Timestamp = parsedTime,
                                        Value = val
                                    });
                                }
                            }
                        }
                        metricsData[prop.Name] = samples;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse provided metrics JSON for saturation detection");
            }
        }

        // If no metrics data was supplied via JSON, query standard resources from metrics service
        if (metricsData.Count == 0)
        {
            var queryResult = await _metricsService.QueryMetricsAsync(standardResources, "15m", "avg", cancellationToken).ConfigureAwait(false);
            if (queryResult?.Metrics != null)
            {
                foreach (var (k, v) in queryResult.Metrics)
                {
                    metricsData[k] = v;
                }
            }
        }

        var result = new ResourceSaturationResult();

        foreach (var (metricName, samples) in metricsData)
        {
            if (samples.Count == 0) continue;

            var current = Math.Round(samples.Last().Value, 2);
            var baseline = await _metricsService.GetBaselineMetricsAsync(service, metricName, 7, cancellationToken).ConfigureAwait(false);
            var baselineP95 = baseline?.P95 ?? 0.0;
            var spikePercent = Math.Round(Math.Max(0, current - baselineP95), 2);

            var item = new ResourceSaturationItem
            {
                Current = current,
                BaselineP95 = baselineP95,
                SpikePercent = spikePercent
            };

            if (metricName.Equals("database_connections", StringComparison.OrdinalIgnoreCase))
            {
                var maxPool = 500.0;
                var available = Math.Max(0, maxPool - current);
                var utilization = Math.Round((current / maxPool) * 100.0, 1);
                item.MaxPool = maxPool;
                item.Available = available;
                item.UtilizationPercent = utilization;

                if (utilization >= thresholdPercent || current >= maxPool * 0.90)
                {
                    result.SaturatedResources.Add(metricName);
                    result.Spikes.Add($"Database connection pool at {utilization}% capacity ({current}/{maxPool})");
                }
            }
            else if (current >= thresholdPercent)
            {
                result.SaturatedResources.Add(metricName);
                result.Spikes.Add($"{metricName} breached saturation threshold with current value {current}% (threshold {thresholdPercent}%)");
            }
            else if (spikePercent > 15.0)
            {
                result.Spikes.Add($"{metricName} spiked {spikePercent}% above normal baseline (current {current}, baseline {baselineP95})");
            }

            result.ResourceSaturation[metricName] = item;
        }

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("correlate_with_incident_time")]
    [Description("Finds metric deviations, timing offsets, and whether metrics led the incident")]
    public string CorrelateWithIncidentTime(
        [Description("Queried metrics JSON string")] string metricsJson,
        [Description("Incident start timestamp (ISO 8601 string)")] string? incidentStart = null)
    {
        _logger.LogInformation("Tool correlate_with_incident_time invoked");

        var incidentTime = DateTime.UtcNow.AddMinutes(-5);
        if (!string.IsNullOrWhiteSpace(incidentStart) && DateTime.TryParse(incidentStart, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var parsedIncident))
        {
            incidentTime = parsedIncident;
        }

        DateTime? anomalyStart = null;
        var correlatedMetrics = new List<string>();

        if (!string.IsNullOrWhiteSpace(metricsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(metricsJson);
                if (doc.RootElement.TryGetProperty("metrics", out var metricsObj))
                {
                    foreach (var metricProp in metricsObj.EnumerateObject())
                    {
                        var metricName = metricProp.Name;
                        DateTime? firstSpikeTime = null;

                        foreach (var sample in metricProp.Value.EnumerateArray())
                        {
                            if (sample.TryGetProperty("value", out var val) && sample.TryGetProperty("timestamp", out var ts))
                            {
                                var v = val.GetDouble();
                                var t = DateTime.Parse(ts.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);

                                // Check if value is abnormally elevated
                                bool isElevated = (metricName.Contains("cpu") && v > 75) ||
                                                 (metricName.Contains("memory") && v > 80) ||
                                                 (metricName.Contains("connection") && v > 350) ||
                                                 (metricName.Contains("disk") && v > 200) ||
                                                 (v > 85);

                                if (isElevated)
                                {
                                    firstSpikeTime ??= t;
                                }
                            }
                        }

                        if (firstSpikeTime.HasValue)
                        {
                            correlatedMetrics.Add(metricName);
                            if (!anomalyStart.HasValue || firstSpikeTime.Value < anomalyStart.Value)
                            {
                                anomalyStart = firstSpikeTime.Value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse metricsJson during time correlation");
            }
        }

        anomalyStart ??= incidentTime;
        var offsetSeconds = (int)(incidentTime - anomalyStart.Value).TotalSeconds;
        var metricsLedIncident = offsetSeconds >= 0;

        return JsonSerializer.Serialize(new
        {
            anomaly_start = anomalyStart.Value.ToString("O"),
            incident_start = incidentTime.ToString("O"),
            offset_seconds = offsetSeconds,
            metrics_led_incident = metricsLedIncident,
            correlated_metrics = correlatedMetrics
        });
    }

    private static List<string> ParseMetricNames(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new List<string> { "cpu_percent", "memory_percent", "database_connections", "disk_io_read_mbps" };
        }

        var list = new List<string>();
        var trimmed = input.Trim();

        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<List<string>>(trimmed);
                if (parsed != null)
                {
                    return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                }
            }
            catch { }
        }

        return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
