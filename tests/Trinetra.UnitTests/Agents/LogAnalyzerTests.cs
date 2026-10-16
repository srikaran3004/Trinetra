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
using Trinetra.Agents.LogAnalyzer;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Logs;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class LogAnalyzerTests
{
    private readonly Mock<ILogQueryService> _mockLogQueryService;
    private readonly Mock<ILogsSnapshotRepository> _mockSnapshotRepo;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<LogAnalyzerPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<LogAnalyzerAgent>> _mockAgentLogger;
    private readonly LogAnalyzerPlugin _plugin;
    private readonly LogAnalyzerAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public LogAnalyzerTests()
    {
        _mockLogQueryService = new Mock<ILogQueryService>();
        _mockSnapshotRepo = new Mock<ILogsSnapshotRepository>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<LogAnalyzerPlugin>>();
        _mockAgentLogger = new Mock<ILogger<LogAnalyzerAgent>>();

        _mockAuditWriter.Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentEvent e, CancellationToken _) =>
            {
                _recordedEvents.Add(e);
                return e;
            });

        _mockSnapshotRepo.Setup(r => r.SaveSnapshotAsync(It.IsAny<LogsSnapshot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LogsSnapshot s, CancellationToken _) => s);

        _plugin = new LogAnalyzerPlugin(_mockLogQueryService.Object, _mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.2,
            MaxTokens = 3000
        };

        _agent = new LogAnalyzerAgent(
            config,
            new Kernel(),
            _mockAgentLogger.Object,
            _plugin,
            _mockSnapshotRepo.Object,
            _mockAuditWriter.Object);
    }

    [Fact]
    public void ExtractStackTraces_ShouldDetectDotNetAndJavaStackTraces()
    {
        var sampleLogs = @"
2026-10-16 14:23:45.123 [Error] Payment failed
System.NullReferenceException: Object reference not set to an instance of an object.
   at Trinetra.Billing.PaymentProcessor.ProcessPayment(Order order) in /app/PaymentProcessor.cs:line 234
   at Trinetra.Billing.CheckoutController.Post(CheckoutRequest req) in /app/CheckoutController.cs:line 88
java.lang.NullPointerException: Cannot invoke method on null
   at com.example.service.UserService.getUser(UserService.java:42)
";

        var json = _plugin.ExtractStackTraces(sampleLogs);
        var result = JsonSerializer.Deserialize<StackTraceExtractionResult>(json);

        result.Should().NotBeNull();
        result!.StackTraces.Should().NotBeEmpty();
        result.Exceptions.Should().Contain(e => e.Contains("NullReferenceException"));
        result.Languages.Should().Contain("C# / .NET");
        result.Languages.Should().Contain("Java");
    }

    [Fact]
    public void DetectAnomalies_WhenErrorRateIsHigh_ShouldClassifyAsCriticalAndClusterErrors()
    {
        var logEntries = new List<object>();
        for (int i = 0; i < 60; i++)
        {
            logEntries.Add(new
            {
                level = "Error",
                message = $"Database connection timeout for user {10000 + i} to host 192.168.1.{i}"
            });
        }
        for (int i = 0; i < 40; i++)
        {
            logEntries.Add(new
            {
                level = "Information",
                message = $"Processed transaction {20000 + i}"
            });
        }

        var logData = JsonSerializer.Serialize(logEntries);

        var json = _plugin.DetectAnomalies(logData);
        var result = JsonSerializer.Deserialize<LogAnomalyDetectionResult>(json);

        result.Should().NotBeNull();
        result!.Severity.Should().Be("critical");
        result.Anomalies.Should().NotBeEmpty();
        result.ErrorPatterns.Should().NotBeEmpty();
        result.ErrorPatterns.First().Should().Contain("Database connection timeout");
    }

    [Fact]
    public void NormalizeLogMessage_ShouldStripTimestampsGuidsAndIps()
    {
        var raw = "2026-10-16T14:23:45Z Connection to 10.0.0.1 failed for session 12345678-1234-1234-1234-123456789abc with code 0x7FFE";
        var normalized = LogAnalyzerPlugin.NormalizeLogMessage(raw);

        normalized.Should().Be("<TIMESTAMP> Connection to <IP> failed for session <UUID> with code <HEX>");
    }

    [Fact]
    public async Task AnalyzeLogsAsync_ShouldQueryLogs_SaveSnapshot_AndRecordAuditEvents()
    {
        var incidentId = Guid.NewGuid();
        var serviceName = "billing-service";

        var fakeLogs = new LogQueryResult
        {
            TotalCount = 25,
            ErrorCount = 5,
            Logs = new List<LogEntry>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Error",
                    Message = "Payment timeout",
                    Service = serviceName,
                    StackTrace = "   at Trinetra.Billing.Payment.Execute() in Payment.cs:line 42"
                },
                new()
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "Information",
                    Message = "Health check OK",
                    Service = serviceName
                }
            }
        };

        _mockLogQueryService.Setup(s => s.QueryLogsAsync(
            serviceName,
            "15m",
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeLogs);

        var report = await _agent.AnalyzeLogsAsync(incidentId, serviceName, "15m");

        // Verify report contents
        report.Should().NotBeNull();
        report.IncidentId.Should().Be(incidentId);
        report.LogsRetrieved.Should().Be(25);
        report.ErrorCount.Should().Be(5);
        report.Summary.Should().Contain("billing-service");
        report.Confidence.Should().BeGreaterThan(0.80m);

        // Verify LogsSnapshot was saved to repository
        _mockSnapshotRepo.Verify(r => r.SaveSnapshotAsync(
            It.Is<LogsSnapshot>(s => s.IncidentId == incidentId && s.LogCount == 25),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Ground Rule: Audit events recorded for start and completion
        _recordedEvents.Should().Contain(e => e.EventType == "log_analysis_started" && e.IncidentId == incidentId);
        _recordedEvents.Should().Contain(e => e.EventType == "log_analysis_completed" && e.IncidentId == incidentId && e.ExecutionStatus == "success");
    }

    [Theory]
    [InlineData("30s", 30)]
    [InlineData("15m", 900)]
    [InlineData("2h", 7200)]
    [InlineData("1d", 86400)]
    public void ParseTimeRange_ShouldParseExpectedSeconds(string input, double expectedSeconds)
    {
        var duration = ElasticsearchLogQueryService.ParseTimeRange(input);
        duration.TotalSeconds.Should().Be(expectedSeconds);
    }
}
