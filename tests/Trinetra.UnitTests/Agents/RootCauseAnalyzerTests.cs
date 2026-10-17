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
using Trinetra.Agents.Coordinator;
using Trinetra.Agents.RootCauseAnalyzer;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Analysis;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class RootCauseAnalyzerTests
{
    private readonly Mock<IIncidentCoordinatorStore> _mockStore;
    private readonly Mock<ILogsSnapshotRepository> _mockLogsRepo;
    private readonly Mock<IMetricsSnapshotRepository> _mockMetricsRepo;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<RootCauseAnalyzerPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<RootCauseAnalyzerAgent>> _mockAgentLogger;
    private readonly RootCauseAnalyzerPlugin _plugin;
    private readonly RootCauseAnalyzerAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public RootCauseAnalyzerTests()
    {
        _mockStore = new Mock<IIncidentCoordinatorStore>();
        _mockLogsRepo = new Mock<ILogsSnapshotRepository>();
        _mockMetricsRepo = new Mock<IMetricsSnapshotRepository>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<RootCauseAnalyzerPlugin>>();
        _mockAgentLogger = new Mock<ILogger<RootCauseAnalyzerAgent>>();

        _mockAuditWriter.Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentEvent e, CancellationToken _) =>
            {
                _recordedEvents.Add(e);
                return e;
            });

        _mockStore.Setup(s => s.UpdateIncidentFindingsAsync(
                It.IsAny<Guid>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _plugin = new RootCauseAnalyzerPlugin(_mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.3,
            MaxTokens = 3000
        };

        _agent = new RootCauseAnalyzerAgent(
            config,
            new Kernel(),
            _mockAgentLogger.Object,
            _plugin,
            _mockStore.Object,
            _mockLogsRepo.Object,
            _mockMetricsRepo.Object,
            _mockAuditWriter.Object);
    }

    [Fact]
    public void CorrelateEvidence_ShouldSynthesizeLogsMetricsAndRunbooks()
    {
        var logsJson = JsonSerializer.Serialize(new
        {
            related_deployments = new[]
            {
                new { service = "auth-api", version = "v2.3.4", changed_files = new[] { "PaymentProcessor.cs" } }
            },
            error_patterns = new[]
            {
                "Npgsql.NpgsqlException: Connection pool exhausted",
                "System.TimeoutException: Database operation timed out"
            }
        });

        var metricsJson = JsonSerializer.Serialize(new
        {
            anomalies = new[] { "Database connection pool at 97% capacity" },
            resource_saturation = new
            {
                database_connections = new { current = 485.0 }
            }
        });

        var runbooksJson = JsonSerializer.Serialize(new
        {
            retrieved_runbooks = new[]
            {
                new { title = "Database Connection Pool Recovery", similarity_score = 0.94 }
            },
            similar_past_incidents = new[]
            {
                new { date = "2026-08-15", title = "Connection pool exhaustion", resolution = "Rolled back deployment" }
            }
        });

        var resultJson = _plugin.CorrelateEvidence(logsJson, metricsJson, runbooksJson);

        resultJson.Should().NotBeNullOrWhiteSpace();
        using var doc = JsonDocument.Parse(resultJson);
        var root = doc.RootElement;

        root.TryGetProperty("evidence_chain", out var ec).Should().BeTrue();
        ec.TryGetProperty("deployment_evidence", out var dep).Should().BeTrue();
        dep.GetProperty("detail").GetString().Should().Contain("auth-api v2.3.4");

        ec.TryGetProperty("logs_evidence", out var log).Should().BeTrue();
        log.GetProperty("detail").GetString().Should().Contain("Connection pool exhausted");

        ec.TryGetProperty("metrics_evidence", out var met).Should().BeTrue();
        met.GetProperty("detail").GetString().Should().Contain("Database connection pool");

        root.TryGetProperty("contributing_factors", out var factors).Should().BeTrue();
        factors.GetArrayLength().Should().BeGreaterThan(0);

        root.TryGetProperty("similar_incidents", out var incidents).Should().BeTrue();
        incidents.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void ClassifyRootCause_ShouldIdentifyDeploymentCategoryWhenDeploymentCorrelates()
    {
        var evidenceJson = JsonSerializer.Serialize(new
        {
            evidence_chain = new
            {
                deployment_evidence = new { detail = "auth-api v2.3.4 deployed recently with unoptimized database query" },
                logs_evidence = new { detail = "Database connection pool timeout errors" },
                metrics_evidence = new { detail = "Database connection pool saturation at 490/500" }
            }
        });

        var resultJson = _plugin.ClassifyRootCause(evidenceJson);
        using var doc = JsonDocument.Parse(resultJson);

        doc.RootElement.GetProperty("root_cause_category").GetString().Should().Be(RootCauseCategories.Deployment);
        doc.RootElement.GetProperty("root_cause").GetString().Should().Contain("deployment");
    }

    [Fact]
    public void ClassifyRootCause_ShouldIdentifyResourceCategoryWhenMetricsOnlyShowSaturation()
    {
        var evidenceJson = JsonSerializer.Serialize(new
        {
            evidence_chain = new
            {
                metrics_evidence = new { detail = "Resource anomalies: CPU saturation at 95%, Memory pressure at 92%" },
                logs_evidence = new { detail = "OutOfMemoryException in worker thread" }
            }
        });

        var resultJson = _plugin.ClassifyRootCause(evidenceJson);
        using var doc = JsonDocument.Parse(resultJson);

        doc.RootElement.GetProperty("root_cause_category").GetString().Should().Be(RootCauseCategories.Resource);
        doc.RootElement.GetProperty("root_cause").GetString().Should().Contain("Resource saturation");
    }

    [Fact]
    public void CalculateConfidence_ShouldAchieveTargetConfidenceForCorroboratedSignals()
    {
        var evidenceJson = JsonSerializer.Serialize(new
        {
            evidence_chain = new
            {
                deployment_evidence = new { confidence = 0.95 },
                logs_evidence = new { confidence = 0.92 },
                metrics_evidence = new { confidence = 0.94 },
                runbook_evidence = new { confidence = 0.90 }
            }
        });

        var resultJson = _plugin.CalculateConfidence(evidenceJson);
        using var doc = JsonDocument.Parse(resultJson);

        var score = doc.RootElement.GetProperty("confidence_score").GetDouble();
        // Target is >= 85% (0.85)
        score.Should().BeGreaterOrEqualTo(0.85);
        score.Should().BeLessOrEqualTo(1.0);
    }

    [Fact]
    public async Task AnalyzeRootCauseAsync_ShouldEnforceAuditTrailAndPersistFindings()
    {
        var incidentId = Guid.NewGuid();
        var logsJson = JsonSerializer.Serialize(new
        {
            related_deployments = new[]
            {
                new { service = "payment-api", version = "v1.2.0", changed_files = new[] { "CheckoutService.cs" } }
            },
            error_patterns = new[] { "Database lock timeout error" }
        });

        var metricsJson = JsonSerializer.Serialize(new
        {
            anomalies = new[] { "Database connection pool near capacity" },
            resource_saturation = new
            {
                database_connections = new { current = 495.0 }
            }
        });

        var report = await _agent.AnalyzeRootCauseAsync(
            incidentId,
            incidentSummary: "Payment checkout failing with database timeout",
            logsJson: logsJson,
            metricsJson: metricsJson);

        report.Should().NotBeNull();
        report.IncidentId.Should().Be(incidentId);
        report.RootCauseCategory.Should().Be(RootCauseCategories.Deployment);
        report.ConfidenceScore.Should().BeGreaterOrEqualTo(0.85m);
        report.ContributingFactors.Should().NotBeEmpty();

        // Verify database findings were updated
        _mockStore.Verify(s => s.UpdateIncidentFindingsAsync(
            incidentId,
            report.RootCause,
            report.RootCauseCategory,
            report.ConfidenceScore,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Ground Rule: Every agent action writes to incident_events
        _recordedEvents.Should().HaveCountGreaterOrEqualTo(2);
        _recordedEvents.Should().Contain(e => e.EventType == "root_cause_analysis_started" && e.IncidentId == incidentId);
        _recordedEvents.Should().Contain(e => e.EventType == "root_cause_analysis_completed" && e.IncidentId == incidentId);

        var completedEvent = _recordedEvents.First(e => e.EventType == "root_cause_analysis_completed");
        completedEvent.ActionDescription.Should().Contain(report.RootCauseCategory);
        completedEvent.AgentName.Should().Be("root_cause_analyzer");
    }

    [Fact]
    public async Task AnalyzeRootCauseAsync_ShouldFallbackToSnapshotsWhenJsonNotPassedDirectly()
    {
        var incidentId = Guid.NewGuid();

        _mockLogsRepo.Setup(r => r.GetSnapshotByIncidentIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LogsSnapshot
            {
                IncidentId = incidentId,
                ErrorPatterns = new List<string> { "PostgreSQL deadlock detected" }
            });

        _mockMetricsRepo.Setup(r => r.GetSnapshotByIncidentIdAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MetricsSnapshot
            {
                IncidentId = incidentId,
                AnomaliesDetected = new List<string> { "Database locks spike" },
                ResourceSaturation = JsonSerializer.Serialize(new
                {
                    database_connections = new { current = 420.0 }
                })
            });

        var report = await _agent.AnalyzeRootCauseAsync(incidentId, "Database deadlock alert");

        report.Should().NotBeNull();
        report.RootCauseCategory.Should().Be(RootCauseCategories.Database);
        report.ConfidenceScore.Should().BeGreaterOrEqualTo(0.80m);

        _mockLogsRepo.Verify(r => r.GetSnapshotByIncidentIdAsync(incidentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockMetricsRepo.Verify(r => r.GetSnapshotByIncidentIdAsync(incidentId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
