using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.MetricsQuerier;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Metrics;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class MetricsQuerierTests
{
    private readonly Mock<IPrometheusMetricsService> _mockMetricsService;
    private readonly Mock<IMetricsSnapshotRepository> _mockSnapshotRepo;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<MetricsQuerierPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<MetricsQuerierAgent>> _mockAgentLogger;
    private readonly MetricsQuerierPlugin _plugin;
    private readonly MetricsQuerierAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public MetricsQuerierTests()
    {
        _mockMetricsService = new Mock<IPrometheusMetricsService>();
        _mockSnapshotRepo = new Mock<IMetricsSnapshotRepository>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<MetricsQuerierPlugin>>();
        _mockAgentLogger = new Mock<ILogger<MetricsQuerierAgent>>();

        _mockAuditWriter.Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentEvent e, CancellationToken _) =>
            {
                _recordedEvents.Add(e);
                return e;
            });

        _mockSnapshotRepo.Setup(r => r.SaveSnapshotAsync(It.IsAny<MetricsSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetricsSnapshot s, CancellationToken _) => s);

        _plugin = new MetricsQuerierPlugin(_mockMetricsService.Object, _mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.2,
            MaxTokens = 2500
        };

        _agent = new MetricsQuerierAgent(
            config,
            new Kernel(),
            _mockAgentLogger.Object,
            _plugin,
            _mockSnapshotRepo.Object,
            _mockAuditWriter.Object);
    }

    [Fact]
    public async Task QueryMetrics_ShouldReturnSerializedMetricsDictionary()
    {
        var now = DateTime.UtcNow;
        _mockMetricsService.Setup(s => s.QueryMetricsAsync(It.IsAny<IEnumerable<string>>(), "15m", "avg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetricQueryResult
            {
                Source = "prometheus",
                TimeRangeStart = now.AddMinutes(-15),
                TimeRangeEnd = now,
                Metrics = new Dictionary<string, List<MetricSample>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["cpu_percent"] = new()
                    {
                        new MetricSample { Timestamp = now.AddMinutes(-5), Value = 85.0 },
                        new MetricSample { Timestamp = now, Value = 92.5 }
                    }
                }
            });

        var json = await _plugin.QueryMetricsAsync("cpu_percent", "15m", "avg");
        json.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("metrics", out var metrics).Should().BeTrue();
        metrics.TryGetProperty("cpu_percent", out var cpu).Should().BeTrue();
        cpu.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetBaselineMetrics_ShouldReturnStatisticalPercentiles()
    {
        _mockMetricsService.Setup(s => s.GetBaselineMetricsAsync("order-service", "cpu_percent", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetricBaselineResult
            {
                Service = "order-service",
                Metric = "cpu_percent",
                P50 = 45.0,
                P95 = 65.0,
                P99 = 75.0,
                Avg = 48.0,
                Min = 10.0,
                Max = 85.0,
                LookbackDays = 7
            });

        var json = await _plugin.GetBaselineMetricsAsync("order-service", "cpu_percent", 7);
        json.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("p95").GetDouble().Should().Be(65.0);
        doc.RootElement.GetProperty("p50").GetDouble().Should().Be(45.0);
    }

    [Fact]
    public async Task DetectSaturation_WhenDbConnectionsExhaustPool_ShouldFlagSaturationAndSpike()
    {
        var now = DateTime.UtcNow;
        _mockMetricsService.Setup(s => s.GetBaselineMetricsAsync("api", "database_connections", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetricBaselineResult { P95 = 200, LookbackDays = 7 });

        var inputMetrics = new
        {
            metrics = new Dictionary<string, object>
            {
                ["database_connections"] = new[]
                {
                    new { timestamp = now.AddMinutes(-5).ToString("O"), value = 400.0 },
                    new { timestamp = now.ToString("O"), value = 485.0 } // 485 / 500 = 97%
                }
            }
        };

        var json = await _plugin.DetectSaturationAsync("api", 90.0, JsonSerializer.Serialize(inputMetrics));
        using var doc = JsonDocument.Parse(json);

        var saturated = doc.RootElement.GetProperty("saturated_resources").EnumerateArray().Select(e => e.GetString()).ToList();
        saturated.Should().Contain("database_connections");

        var spikes = doc.RootElement.GetProperty("spikes").EnumerateArray().Select(e => e.GetString()).ToList();
        spikes.Should().Contain(s => s!.Contains("Database connection pool at 97% capacity"));
    }

    [Fact]
    public void CorrelateWithIncidentTime_WhenMetricsSpikeBeforeIncident_ShouldIndicateMetricsLed()
    {
        var incidentTime = DateTime.UtcNow;
        var anomalyTime = incidentTime.AddMinutes(-3);

        var metricsJson = JsonSerializer.Serialize(new
        {
            metrics = new Dictionary<string, object>
            {
                ["cpu_percent"] = new[]
                {
                    new { timestamp = anomalyTime.ToString("O"), value = 95.0 }
                }
            }
        });

        var json = _plugin.CorrelateWithIncidentTime(metricsJson, incidentTime.ToString("O"));
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("metrics_led_incident").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("offset_seconds").GetInt32().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("correlated_metrics").EnumerateArray().Select(e => e.GetString()).Should().Contain("cpu_percent");
    }

    [Fact]
    public async Task QueryAndAnalyzeMetricsAsync_ShouldEnforceAuditTrailGroundRuleAndSaveSnapshot()
    {
        var incidentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _mockMetricsService.Setup(s => s.QueryMetricsAsync(It.IsAny<IEnumerable<string>>(), "15m", "avg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetricQueryResult
            {
                Source = "prometheus",
                TimeRangeStart = now.AddMinutes(-15),
                TimeRangeEnd = now,
                Metrics = new Dictionary<string, List<MetricSample>>(StringComparer.OrdinalIgnoreCase)
                {
                    ["cpu_percent"] = new()
                    {
                        new MetricSample { Timestamp = now.AddMinutes(-5), Value = 80.0 },
                        new MetricSample { Timestamp = now, Value = 94.0 }
                    },
                    ["database_connections"] = new()
                    {
                        new MetricSample { Timestamp = now, Value = 485.0 }
                    }
                }
            });

        _mockMetricsService.Setup(s => s.GetBaselineMetricsAsync(It.IsAny<string>(), It.IsAny<string>(), 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetricBaselineResult { P95 = 65, LookbackDays = 7 });

        var report = await _agent.QueryAndAnalyzeMetricsAsync(incidentId, "payment-api", "15m", now);

        // Ground Rule Verification: Audit events must be written on agent actions
        _recordedEvents.Should().HaveCount(2);
        _recordedEvents[0].EventType.Should().Be("metrics_query_started");
        _recordedEvents[1].EventType.Should().Be("metrics_query_completed");
        _recordedEvents[0].AgentName.Should().Be(MetricsQuerierAgent.AgentIdentifier);
        _recordedEvents[1].AgentName.Should().Be(MetricsQuerierAgent.AgentIdentifier);

        // Verification: Snapshot saved
        _mockSnapshotRepo.Verify(r => r.SaveSnapshotAsync(It.Is<MetricsSnapshot>(s =>
            s.IncidentId == incidentId &&
            s.Source == "prometheus" &&
            s.MetricNames.Contains("cpu_percent")), It.IsAny<CancellationToken>()), Times.Once);

        // Verification: Report output structure
        report.IncidentId.Should().Be(incidentId);
        report.Confidence.Should().BeGreaterThanOrEqualTo(0.85m);
        report.ResourceSaturation.Should().NotBeEmpty();
        report.Anomalies.Should().NotBeEmpty();
        report.Summary.Should().Contain("payment-api");
    }
}
