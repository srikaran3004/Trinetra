using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Pgvector;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Coordinator;
using Trinetra.Agents.PostMortem;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.PostMortem;
using Trinetra.Infrastructure.Data;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class PostMortemTests
{
    private readonly Mock<IIncidentCoordinatorStore> _mockStore;
    private readonly Mock<IIncidentHistoryRepository> _mockHistoryRepo;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<PostMortemPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<PostMortemAgent>> _mockAgentLogger;
    private readonly PostMortemPlugin _plugin;
    private readonly PostMortemAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();
    private IncidentHistory? _savedHistory;

    public PostMortemTests()
    {
        _mockStore = new Mock<IIncidentCoordinatorStore>();
        _mockHistoryRepo = new Mock<IIncidentHistoryRepository>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<PostMortemPlugin>>();
        _mockAgentLogger = new Mock<ILogger<PostMortemAgent>>();

        _mockAuditWriter.Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentEvent e, CancellationToken _) =>
            {
                _recordedEvents.Add(e);
                return e;
            });

        _mockEmbeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        _mockHistoryRepo.Setup(h => h.SaveIncidentHistoryAsync(It.IsAny<IncidentHistory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentHistory h, CancellationToken _) =>
            {
                _savedHistory = h;
                return h;
            });

        _plugin = new PostMortemPlugin(_mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.2,
            MaxTokens = 2500,
            TimeoutSeconds = 120
        };

        var kernel = Kernel.CreateBuilder().Build();

        _agent = new PostMortemAgent(
            config,
            kernel,
            _mockAgentLogger.Object,
            _plugin,
            _mockStore.Object,
            _mockHistoryRepo.Object,
            _mockEmbeddingService.Object,
            _mockAuditWriter.Object);
    }

    [Fact]
    public async Task GeneratePostMortemAsync_GeneratesStructuredMarkdownWithRequiredSections()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var triggeredAt = DateTime.UtcNow.AddMinutes(-45);
        var resolvedAt = DateTime.UtcNow;

        var incident = new Incident
        {
            Id = incidentId,
            Title = "High CPU and Connection Pool Exhaustion on Orders DB",
            Severity = "critical",
            Status = "resolved",
            TriggeredAt = triggeredAt,
            ResolvedAt = resolvedAt,
            AffectedServices = new List<string> { "order-service", "payment-service" },
            RootCause = "Unindexed query in order status polling exhausted PostgreSQL connection pool",
            RootCauseCategory = "DATABASE",
            RootCauseConfidence = 0.92m,
            ProposedRemediation = "Terminated idle blocker queries and deployed hotfix index",
            Events = new List<IncidentEvent>
            {
                new()
                {
                    EventType = "incident_triggered",
                    AgentName = "Coordinator",
                    ActionDescription = "Alert received from Prometheus",
                    CreatedAt = triggeredAt
                },
                new()
                {
                    EventType = "remediation_executed",
                    AgentName = "AutoExecutor",
                    ActionDescription = "Killed long running queries",
                    CreatedAt = triggeredAt.AddMinutes(15)
                },
                new()
                {
                    EventType = "incident_resolved",
                    AgentName = "AutoExecutor",
                    ActionDescription = "Metrics verified normal",
                    CreatedAt = resolvedAt
                }
            }
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        var report = await _agent.GeneratePostMortemAsync(incidentId);

        // Assert
        report.Should().NotBeNull();
        report.IncidentId.Should().Be(incidentId);
        report.Title.Should().Be(incident.Title);
        report.Severity.Should().Be("critical");
        report.TotalDurationMinutes.Should().BeGreaterThanOrEqualTo(40);
        report.EventsCount.Should().Be(3);

        // Verify Markdown structure
        report.MarkdownReport.Should().Contain("# Incident Post-Mortem:");
        report.MarkdownReport.Should().Contain("## 1. Executive Summary");
        report.MarkdownReport.Should().Contain("## 2. Root Cause Analysis");
        report.MarkdownReport.Should().Contain("## 3. Remediation & Recovery Actions");
        report.MarkdownReport.Should().Contain("## 4. Incident Timeline");
        report.MarkdownReport.Should().Contain("## 5. Preventative Action Items");
        report.MarkdownReport.Should().Contain("## 6. Lessons Learned");

        // Action items check
        report.ActionItems.Should().NotBeEmpty();
        report.ActionItems.Should().Contain(i => i.Contains("connection pool", StringComparison.OrdinalIgnoreCase) || i.Contains("query", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GeneratePostMortemAsync_ComputesEmbeddingAndSavesToIncidentHistory()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Memory leak in auth cache",
            Severity = "high",
            Status = "resolved",
            TriggeredAt = DateTime.UtcNow.AddMinutes(-30),
            ResolvedAt = DateTime.UtcNow,
            RootCause = "Unbounded dictionary in session store",
            RootCauseCategory = "MEMORY_LEAK",
            ProposedRemediation = "Restarted pod and flushed redis session cache",
            Events = new List<IncidentEvent>()
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        var report = await _agent.GeneratePostMortemAsync(incidentId);

        // Assert
        _mockEmbeddingService.Verify(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockHistoryRepo.Verify(h => h.SaveIncidentHistoryAsync(It.IsAny<IncidentHistory>(), It.IsAny<CancellationToken>()), Times.Once);

        _savedHistory.Should().NotBeNull();
        _savedHistory!.OriginalIncidentId.Should().Be(incidentId);
        _savedHistory.RootCauseCategory.Should().Be("MEMORY_LEAK");
        _savedHistory.HistoryEmbedding.Should().NotBeNull();
    }

    [Fact]
    public async Task GeneratePostMortemAsync_EnforcesAuditTrailGroundRule()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "API Gateway 504 Gateway Timeout",
            Severity = "high",
            Status = "resolved",
            TriggeredAt = DateTime.UtcNow.AddMinutes(-20),
            ResolvedAt = DateTime.UtcNow,
            Events = new List<IncidentEvent>()
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Act
        await _agent.GeneratePostMortemAsync(incidentId);

        // Assert
        _recordedEvents.Should().Contain(e => e.EventType == "post_mortem_generation_started");
        _recordedEvents.Should().Contain(e => e.EventType == "post_mortem_generation_completed");
    }

    [Fact]
    public async Task DbIncidentHistoryRepository_CanSaveAndSearchWithInMemoryFallback()
    {
        // Arrange: In-memory DbContext
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var loggerMock = new Mock<ILogger<DbIncidentHistoryRepository>>();
        var repo = new DbIncidentHistoryRepository(dbContext, loggerMock.Object);

        var hist1 = new IncidentHistory
        {
            Title = "Postgres Connection Exhaustion",
            RootCause = "Too many idle connections",
            RootCauseCategory = "DATABASE",
            Severity = "critical",
            HistoryEmbedding = new Vector(new float[] { 1.0f, 0.0f, 0.0f }),
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var hist2 = new IncidentHistory
        {
            Title = "Redis Cache Cluster Out of Memory",
            RootCause = "Cache eviction policy misconfigured",
            RootCauseCategory = "MEMORY_LEAK",
            Severity = "high",
            HistoryEmbedding = new Vector(new float[] { 0.0f, 1.0f, 0.0f }),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await repo.SaveIncidentHistoryAsync(hist1);
        await repo.SaveIncidentHistoryAsync(hist2);

        // Act: Search for vector close to hist1
        var searchVector = new float[] { 0.95f, 0.05f, 0.0f };
        var results = await repo.SearchSimilarIncidentsAsync(searchVector, limit: 1);

        // Assert
        results.Should().HaveCount(1);
        results[0].Title.Should().Be("Postgres Connection Exhaustion");
    }
}
