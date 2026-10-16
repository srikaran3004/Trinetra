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
using Pgvector;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.RunbookRetriever;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Runbooks;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class RunbookRetrieverTests
{
    private readonly Mock<IRunbookRepository> _mockRunbookRepo;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<RunbookRetrieverPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<RunbookRetrieverAgent>> _mockAgentLogger;
    private readonly RunbookRetrieverPlugin _plugin;
    private readonly RunbookRetrieverAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public RunbookRetrieverTests()
    {
        _mockRunbookRepo = new Mock<IRunbookRepository>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<RunbookRetrieverPlugin>>();
        _mockAgentLogger = new Mock<ILogger<RunbookRetrieverAgent>>();

        _mockAuditWriter.Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentEvent e, CancellationToken _) =>
            {
                _recordedEvents.Add(e);
                return e;
            });

        _mockEmbeddingService.Setup(e => e.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[1536]);

        _plugin = new RunbookRetrieverPlugin(_mockEmbeddingService.Object, _mockRunbookRepo.Object, _mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.1,
            MaxTokens = 2000
        };

        _agent = new RunbookRetrieverAgent(
            config,
            new Kernel(),
            _mockAgentLogger.Object,
            _plugin,
            _mockAuditWriter.Object);
    }

    [Fact]
    public async Task EmbeddingService_ShouldProduce1536DimensionalNormalizedVector()
    {
        var loggerMock = new Mock<ILogger<EmbeddingService>>();
        var service = new EmbeddingService(loggerMock.Object);

        var vector1 = await service.GenerateEmbeddingAsync("database connection pool exhausted");
        var vector2 = await service.GenerateEmbeddingAsync("database connection pool exhausted");
        var vector3 = await service.GenerateEmbeddingAsync("completely unrelated topic about flowers in spring");

        vector1.Should().HaveCount(1536);
        vector2.Should().HaveCount(1536);

        // Vector 1 and Vector 2 are identical text, dot product should be 1.0 (exact match)
        double dotIdentical = 0;
        for (int i = 0; i < 1536; i++) dotIdentical += vector1[i] * vector2[i];
        dotIdentical.Should().BeApproximately(1.0, 0.001);

        // Vector 1 and Vector 3 are unrelated, similarity should be substantially lower
        double dotUnrelated = 0;
        for (int i = 0; i < 1536; i++) dotUnrelated += vector1[i] * vector3[i];
        dotUnrelated.Should().BeLessThan(0.7);
    }

    [Fact]
    public async Task SearchRunbooksByEmbedding_ShouldReturnMatchingRunbooksWithScores()
    {
        var runbookId = Guid.NewGuid();
        var sampleRunbook = new Runbook
        {
            Id = runbookId,
            Title = "Database Connection Pool Exhaustion Recovery",
            Description = "Procedures to recover from pool starvation",
            ApplicableServices = new() { "auth-api", "payment-api" },
            EstimatedTimeMinutes = 10,
            RisksAndWarnings = new() { "Requires SRE approval" }
        };

        _mockRunbookRepo.Setup(r => r.SearchByEmbeddingAsync(It.IsAny<float[]>(), 5, 0.5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Runbook, double)> { (sampleRunbook, 0.92) });

        var json = await _plugin.SearchRunbooksByEmbeddingAsync("postgres pool exhausted", 5);
        using var doc = JsonDocument.Parse(json);

        var runbooks = doc.RootElement.GetProperty("runbooks").EnumerateArray().ToList();
        runbooks.Should().HaveCount(1);
        runbooks[0].GetProperty("title").GetString().Should().Be("Database Connection Pool Exhaustion Recovery");
        runbooks[0].GetProperty("similarity_score").GetDouble().Should().Be(0.92);
    }

    [Fact]
    public async Task SearchIncidentHistory_ShouldReturnPastIncidentsAndAppliedSolutions()
    {
        var history = new IncidentHistory
        {
            Title = "Database connection pool exhaustion",
            RootCause = "Unoptimized query in recent deployment",
            RemediationAction = "Rolled back deployment",
            ResolutionTimeMinutes = 8,
            OccurredAt = DateTime.UtcNow.AddDays(-10)
        };

        _mockRunbookRepo.Setup(r => r.SearchIncidentHistoryAsync(It.IsAny<float[]>(), It.IsAny<string>(), 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(IncidentHistory, double)> { (history, 0.89) });

        var json = await _plugin.SearchIncidentHistoryAsync("connection timeout pool full", 3);
        using var doc = JsonDocument.Parse(json);

        var past = doc.RootElement.GetProperty("past_incidents").EnumerateArray().ToList();
        past.Should().HaveCount(1);
        past[0].GetProperty("resolution_applied").GetString().Should().Be("Rolled back deployment");
        past[0].GetProperty("similarity_score").GetDouble().Should().Be(0.89);
    }

    [Fact]
    public async Task ExtractRemediationSteps_WhenMarkdownHasSteps_ShouldParseStructuredSteps()
    {
        var runbookId = Guid.NewGuid();
        var markdown = @"
# Database Pool Recovery
### Step 1: Check connection count
```sql
SELECT count(*) FROM pg_stat_activity;
```
### Step 2: Terminate idle connections (medium risk)
```sql
SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle';
```
";
        var runbook = new Runbook
        {
            Id = runbookId,
            Title = "Database Pool Recovery",
            Content = markdown,
            EstimatedTimeMinutes = 15,
            RisksAndWarnings = new() { "May terminate active connections" }
        };

        _mockRunbookRepo.Setup(r => r.GetRunbookByIdAsync(runbookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        var json = await _plugin.ExtractRemediationStepsAsync(runbookId.ToString());
        using var doc = JsonDocument.Parse(json);

        var steps = doc.RootElement.GetProperty("steps").EnumerateArray().ToList();
        steps.Should().HaveCount(2);
        steps[0].GetProperty("step").GetInt32().Should().Be(1);
        steps[0].GetProperty("command").GetString().Should().Contain("pg_stat_activity");
        steps[1].GetProperty("risk").GetString().Should().Be("medium");
    }

    [Fact]
    public async Task RetrieveRunbooksAsync_ShouldEnforceAuditTrailGroundRuleAndSynthesizeReport()
    {
        var incidentId = Guid.NewGuid();
        var runbookId = Guid.NewGuid();

        var runbook = new Runbook
        {
            Id = runbookId,
            Title = "High CPU Throttling Mitigation",
            ApplicableServices = new() { "order-service" },
            EstimatedTimeMinutes = 5,
            Content = @"
### Step 1: Scale deployment replicas
```bash
kubectl scale deployment order-service --replicas=5
```
",
            RisksAndWarnings = new() { "Increases cloud costs" }
        };

        _mockRunbookRepo.Setup(r => r.SearchByEmbeddingAsync(It.IsAny<float[]>(), 5, 0.5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Runbook, double)> { (runbook, 0.91) });

        _mockRunbookRepo.Setup(r => r.GetRunbookByIdAsync(runbookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runbook);

        var report = await _agent.RetrieveRunbooksAsync(
            incidentId,
            "CPU usage pegged at 95% on order-service",
            new List<string> { "CPUThrottlingHigh" },
            "order-service");

        // Ground Rule Verification: Audit events must be written
        _recordedEvents.Should().HaveCount(2);
        _recordedEvents[0].EventType.Should().Be("runbook_retrieval_started");
        _recordedEvents[1].EventType.Should().Be("runbook_retrieval_completed");
        _recordedEvents[0].AgentName.Should().Be(RunbookRetrieverAgent.AgentIdentifier);
        _recordedEvents[1].AgentName.Should().Be(RunbookRetrieverAgent.AgentIdentifier);

        // Report Verification
        report.IncidentId.Should().Be(incidentId);
        report.Confidence.Should().BeGreaterThanOrEqualTo(0.90m);
        report.RetrievedRunbooks.Should().NotBeEmpty();
        report.RetrievedRunbooks[0].Title.Should().Be("High CPU Throttling Mitigation");
        report.RetrievedRunbooks[0].RemediationSteps.Should().NotBeEmpty();
        report.Summary.Should().Contain("High CPU Throttling Mitigation");
    }
}
