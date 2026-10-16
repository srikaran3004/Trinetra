using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Coordinator;
using Trinetra.Agents.Factory;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class IncidentCoordinatorTests
{
    private readonly Mock<IIncidentCoordinatorStore> _mockStore;
    private readonly Mock<IAgentInvoker> _mockInvoker;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<CoordinatorPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<IncidentCoordinatorAgent>> _mockAgentLogger;
    private readonly CoordinatorPlugin _plugin;
    private readonly IncidentCoordinatorAgent _coordinator;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public IncidentCoordinatorTests()
    {
        _mockStore = new Mock<IIncidentCoordinatorStore>();
        _mockInvoker = new Mock<IAgentInvoker>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<CoordinatorPlugin>>();
        _mockAgentLogger = new Mock<ILogger<IncidentCoordinatorAgent>>();

        _mockAuditWriter
            .Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .Callback<IncidentEvent, CancellationToken>((ev, ct) => _recordedEvents.Add(ev))
            .ReturnsAsync((IncidentEvent ev, CancellationToken ct) => ev);

        _plugin = new CoordinatorPlugin(_mockStore.Object, _mockInvoker.Object, _mockAuditWriter.Object, _mockPluginLogger.Object);

        var factory = new SemanticKernelFactory();
        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.3,
            MaxTokens = 2000,
            TimeoutSeconds = 300
        };
        var kernel = factory.CreateKernel(config);

        _coordinator = new IncidentCoordinatorAgent(
            config,
            kernel,
            _mockAgentLogger.Object,
            _mockStore.Object,
            _mockInvoker.Object,
            _plugin,
            _mockAuditWriter.Object);
    }

    [Theory]
    [InlineData("triggered", "investigating", true)]
    [InlineData("triggered", "escalated", true)]
    [InlineData("triggered", "resolved", false)]
    [InlineData("investigating", "analysis_complete", true)]
    [InlineData("investigating", "executing", false)]
    [InlineData("analysis_complete", "awaiting_approval", true)]
    [InlineData("analysis_complete", "executing", true)]
    [InlineData("awaiting_approval", "executing", true)]
    [InlineData("executing", "monitoring", true)]
    [InlineData("monitoring", "resolved", true)]
    [InlineData("executing", "resolved", true)]
    [InlineData("investigating", "escalated", true)]
    public void IncidentStateMachine_Transitions_ShouldMatchSpec(string from, string to, bool expected)
    {
        var canTransition = IncidentStateMachine.CanTransition(from, to);
        canTransition.Should().Be(expected);

        if (!expected)
        {
            var act = () => IncidentStateMachine.ValidateTransition(from, to);
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [Fact]
    public async Task CoordinatorPlugin_CreateIncident_ShouldPersistAndLogAuditEvent()
    {
        var incidentId = Guid.NewGuid();
        _mockStore
            .Setup(s => s.CreateIncidentAsync(It.IsAny<Incident>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Incident inc, CancellationToken ct) =>
            {
                inc.Id = incidentId;
                return inc;
            });

        var resultJson = await _plugin.CreateIncidentAsync(
            title: "Database latency spike",
            source: "prometheus",
            severity: "high",
            description: "P99 latency > 2s",
            affectedServicesJson: "[\"payment-api\", \"auth-api\"]");

        resultJson.Should().Contain(incidentId.ToString());
        resultJson.Should().Contain("triggered");

        _recordedEvents.Should().ContainSingle(e =>
            e.EventType == "incident_created" &&
            e.IncidentId == incidentId &&
            e.AgentName == "IncidentCoordinator");
    }

    [Fact]
    public async Task CoordinatorPlugin_UpdateStatus_ShouldValidateTransitionAndLogAuditEvent()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Status = IncidentStatuses.Triggered
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);
        _mockStore.Setup(s => s.UpdateIncidentStatusAsync(incidentId, IncidentStatuses.Investigating, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var resultJson = await _plugin.UpdateIncidentStatusAsync(
            incidentId.ToString(),
            IncidentStatuses.Investigating,
            "Starting agent investigation");

        resultJson.Should().Contain("\"success\":true");
        resultJson.Should().Contain("investigating");

        _recordedEvents.Should().Contain(e =>
            e.EventType == "state_changed" &&
            e.IncidentId == incidentId &&
            e.ActionDescription!.Contains("triggered", StringComparison.OrdinalIgnoreCase) &&
            e.ActionDescription.Contains("investigating", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CoordinatorPlugin_UpdateStatus_WithInvalidTransition_ShouldFailGracefully()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Status = IncidentStatuses.Triggered
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        // Triggered cannot transition directly to executing
        var resultJson = await _plugin.UpdateIncidentStatusAsync(
            incidentId.ToString(),
            IncidentStatuses.Executing,
            "Invalid jump");

        resultJson.Should().Contain("\"success\":false");
        resultJson.Should().Contain("Invalid incident state transition");
    }

    [Fact]
    public async Task CoordinatorPlugin_InvokeAgent_ShouldInvokeAndLogEvents()
    {
        var incidentId = Guid.NewGuid();
        _mockInvoker
            .Setup(i => i.InvokeAgentAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AgentExecutionResult.Succeeded("log_analyzer", new { errors_found = 12 }));

        var resultJson = await _plugin.InvokeAgentAsync(
            agentName: "log_analyzer",
            incidentId: incidentId.ToString(),
            action: "query_logs",
            parametersJson: "{\"service\":\"auth-api\"}");

        resultJson.Should().Contain("log_analyzer");
        resultJson.Should().Contain("\"Success\":true");

        // Ground Rule: invocation start AND completion must be logged
        _recordedEvents.Should().Contain(e => e.EventType == "agent_invoked" && e.AgentName == "IncidentCoordinator");
        _recordedEvents.Should().Contain(e => e.EventType == "agent_completed" && e.AgentName == "log_analyzer");
    }

    [Fact]
    public async Task CoordinateInvestigationAsync_ShouldRunParallelAgentsAndCompleteWorkflow()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "API gateway 502 spike",
            Source = "prometheus",
            Severity = IncidentSeverities.High,
            Status = IncidentStatuses.Triggered,
            AffectedServices = new List<string> { "api-gateway" },
            TriggeredAt = DateTime.UtcNow
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _mockStore.Setup(s => s.UpdateIncidentStatusAsync(incidentId, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, string, string, CancellationToken>((id, status, reason, ct) => incident.Status = status)
            .ReturnsAsync(true);

        _mockInvoker
            .Setup(i => i.InvokeAgentAsync(It.IsAny<AgentInvocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AgentInvocationRequest req, CancellationToken ct) =>
                AgentExecutionResult.Succeeded(req.AgentName, new { findings = $"{req.AgentName} findings" }, 45));

        // Act
        var report = await _coordinator.CoordinateInvestigationAsync(incidentId, timeoutSeconds: 30);

        // Assert
        report.Should().NotBeNull();
        report.Success.Should().BeTrue();
        report.Status.Should().Be(IncidentStatuses.AnalysisComplete);

        // All 3 parallel investigation agents must have executed
        report.AgentResults.Should().ContainKey("log_analyzer");
        report.AgentResults.Should().ContainKey("metrics_querier");
        report.AgentResults.Should().ContainKey("runbook_retriever");

        report.AgentResults["log_analyzer"].Success.Should().BeTrue();
        report.AgentResults["metrics_querier"].Success.Should().BeTrue();
        report.AgentResults["runbook_retriever"].Success.Should().BeTrue();

        // Audit Trail Ground Rule: Multiple audit events logged along the workflow
        _recordedEvents.Should().Contain(e => e.EventType == "investigation_started" && e.IncidentId == incidentId);
        _recordedEvents.Should().Contain(e => e.EventType == "state_changed" && e.ActionDescription!.Contains("investigating", StringComparison.OrdinalIgnoreCase));
        _recordedEvents.Should().Contain(e => e.EventType == "state_changed" && e.ActionDescription!.Contains("analysis_complete", StringComparison.OrdinalIgnoreCase));
        _recordedEvents.Should().Contain(e => e.EventType == "investigation_completed" && e.IncidentId == incidentId);
    }

    [Fact]
    public async Task CoordinateInvestigationAsync_WhenStoreFails_ShouldEscalateIncident()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Status = IncidentStatuses.Triggered
        };

        _mockStore.Setup(s => s.GetIncidentAsync(incidentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(incident);

        _mockStore.Setup(s => s.UpdateIncidentStatusAsync(incidentId, IncidentStatuses.Investigating, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        _mockStore.Setup(s => s.EscalateIncidentAsync(incidentId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var report = await _coordinator.CoordinateInvestigationAsync(incidentId, timeoutSeconds: 10);

        report.Success.Should().BeFalse();
        report.Status.Should().Be(IncidentStatuses.Escalated);
        report.ErrorMessage.Should().Contain("Database connection failed");

        _recordedEvents.Should().Contain(e => e.EventType == "incident_escalated" && e.IncidentId == incidentId);
    }
}
