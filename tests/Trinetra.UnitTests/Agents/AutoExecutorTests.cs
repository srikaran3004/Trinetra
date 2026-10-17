using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Moq;
using Trinetra.Agents.AutoExecutor;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Coordinator;
using Trinetra.Agents.Execution;
using Trinetra.Agents.Guardrails;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Execution;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class AutoExecutorTests
{
    private readonly Mock<ICommandSandbox> _mockSandbox;
    private readonly Mock<IIncidentCoordinatorStore> _mockStore;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<AutoExecutorPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<AutoExecutorAgent>> _mockAgentLogger;
    private readonly IGuardrailsEngine _guardrailsEngine;
    private readonly AutoExecutorPlugin _plugin;
    private readonly AutoExecutorAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public AutoExecutorTests()
    {
        _mockSandbox = new Mock<ICommandSandbox>();
        _mockStore = new Mock<IIncidentCoordinatorStore>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<AutoExecutorPlugin>>();
        _mockAgentLogger = new Mock<ILogger<AutoExecutorAgent>>();
        _guardrailsEngine = new GuardrailsEngine();

        _mockAuditWriter.Setup(w => w.WriteEventAsync(It.IsAny<IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncidentEvent e, CancellationToken _) =>
            {
                _recordedEvents.Add(e);
                return e;
            });

        _mockStore.Setup(s => s.UpdateIncidentStatusAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockStore.Setup(s => s.EscalateIncidentAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _plugin = new AutoExecutorPlugin(_mockSandbox.Object, _mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.1,
            MaxTokens = 2000,
            TimeoutSeconds = 300
        };

        var kernel = Kernel.CreateBuilder().Build();

        _agent = new AutoExecutorAgent(
            config,
            kernel,
            _mockAgentLogger.Object,
            _plugin,
            _guardrailsEngine,
            _mockSandbox.Object,
            _mockStore.Object,
            _mockAuditWriter.Object);
    }

    [Fact]
    public async Task ExecuteRemediationAsync_WhenCommandBlockedByGuardrail_BlocksExecutionAndEscalates()
    {
        // Arrange: destructive rm -rf /
        var incidentId = Guid.NewGuid();
        var request = new CommandExecutionRequest
        {
            IncidentId = incidentId,
            ActionTitle = "Dangerous cleanup",
            Command = "rm -rf /",
            RiskLevel = "low"
        };

        // Act
        var result = await _agent.ExecuteRemediationAsync(request);

        // Assert
        result.Status.Should().Be(ExecutionStatus.Blocked);
        result.ErrorMessage.Should().Contain("Blocked by guardrail");

        // Ground Rule: Sandbox must NEVER be called for blocked commands
        _mockSandbox.Verify(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);

        // Incident must be escalated
        _mockStore.Verify(s => s.EscalateIncidentAsync(incidentId, It.Is<string>(r => r.Contains("guardrail")), It.IsAny<CancellationToken>()), Times.Once);

        // Audit Trail Ground Rule: guardrail_violation must be recorded
        _recordedEvents.Should().Contain(e => e.EventType == "guardrail_violation");
    }

    [Fact]
    public async Task ExecuteRemediationAsync_WhenRequiresApproval_AndNotApproved_BlocksExecution()
    {
        // Arrange: kubectl scale requires approval
        var incidentId = Guid.NewGuid();
        var request = new CommandExecutionRequest
        {
            IncidentId = incidentId,
            ActionTitle = "Scale pods",
            Command = "kubectl scale deployment myapp --replicas=3",
            RiskLevel = "medium",
            IsApproved = false
        };

        // Act
        var result = await _agent.ExecuteRemediationAsync(request);

        // Assert
        result.Status.Should().Be(ExecutionStatus.Blocked);
        _mockSandbox.Verify(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);

        _recordedEvents.Should().Contain(e => e.EventType == "approval_required_unmet");
        _mockStore.Verify(s => s.UpdateIncidentStatusAsync(incidentId, IncidentStatuses.AwaitingApproval, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteRemediationAsync_WhenRequiresApproval_AndApproved_ExecutesCommandSuccessfully()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var request = new CommandExecutionRequest
        {
            IncidentId = incidentId,
            ActionTitle = "Scale pods approved",
            Command = "kubectl scale deployment myapp --replicas=3",
            RiskLevel = "medium",
            IsApproved = true
        };

        _mockSandbox.Setup(s => s.ExecuteCommandAsync("kubectl scale deployment myapp --replicas=3", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutput
            {
                ExitCode = 0,
                StandardOutput = "deployment.apps/myapp scaled",
                DurationMs = 450
            });

        // Act
        var result = await _agent.ExecuteRemediationAsync(request);

        // Assert
        result.Status.Should().Be(ExecutionStatus.Success);
        result.ExecutionSteps.Should().HaveCount(1);
        result.ExecutionSteps[0].StandardOutput.Should().Be("deployment.apps/myapp scaled");

        _mockStore.Verify(s => s.UpdateIncidentStatusAsync(incidentId, IncidentStatuses.Executing, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockStore.Verify(s => s.UpdateIncidentStatusAsync(incidentId, IncidentStatuses.Resolved, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);

        _recordedEvents.Should().Contain(e => e.EventType == "remediation_execution_completed");
    }

    [Fact]
    public async Task ExecuteRemediationAsync_WhenPreflightFails_AbortsAndEscalates()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var request = new CommandExecutionRequest
        {
            IncidentId = incidentId,
            ActionTitle = "Restart service",
            Command = "whoami",
            PreflightChecks = new List<string> { "abort if system degraded" },
            IsApproved = true
        };

        // Act
        var result = await _agent.ExecuteRemediationAsync(request);

        // Assert
        result.Status.Should().Be(ExecutionStatus.Failed);
        result.ErrorMessage.Should().Contain("Pre-flight check failed");

        _mockSandbox.Verify(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockStore.Verify(s => s.EscalateIncidentAsync(incidentId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _recordedEvents.Should().Contain(e => e.EventType == "preflight_check_failed");
    }

    [Fact]
    public async Task ExecuteRemediationAsync_WhenCommandFails_ExecutesRollbackAndEscalates()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var request = new CommandExecutionRequest
        {
            IncidentId = incidentId,
            ActionTitle = "Restart service",
            Command = "kubectl rollout restart deployment/myapp",
            RollbackCommand = "kubectl rollout undo deployment/myapp",
            IsApproved = true
        };

        _mockSandbox.Setup(s => s.ExecuteCommandAsync("kubectl rollout restart deployment/myapp", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutput
            {
                ExitCode = 1,
                StandardError = "error: deployment not found",
                DurationMs = 200
            });

        _mockSandbox.Setup(s => s.ExecuteCommandAsync("kubectl rollout undo deployment/myapp", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutput
            {
                ExitCode = 0,
                StandardOutput = "rollback successful",
                DurationMs = 300
            });

        // Act
        var result = await _agent.ExecuteRemediationAsync(request);

        // Assert
        result.Status.Should().Be(ExecutionStatus.Failed);
        result.RollbackExecuted.Should().BeTrue();
        result.RollbackSuccess.Should().BeTrue();

        _mockStore.Verify(s => s.EscalateIncidentAsync(incidentId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _recordedEvents.Should().Contain(e => e.EventType == "remediation_rollback_executed");
    }

    [Fact]
    public async Task ExecuteRemediationAsync_WhenPostflightFails_ExecutesRollbackAndEscalates()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var request = new CommandExecutionRequest
        {
            IncidentId = incidentId,
            ActionTitle = "Patch config",
            Command = "kubectl apply -f patch.yaml",
            RollbackCommand = "kubectl apply -f original.yaml",
            PostflightChecks = new List<string> { "verify service not degraded" },
            IsApproved = true
        };

        _mockSandbox.Setup(s => s.ExecuteCommandAsync("kubectl apply -f patch.yaml", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutput
            {
                ExitCode = 0,
                StandardOutput = "configured",
                DurationMs = 150
            });

        _mockSandbox.Setup(s => s.ExecuteCommandAsync("kubectl apply -f original.yaml", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommandOutput
            {
                ExitCode = 0,
                StandardOutput = "restored",
                DurationMs = 200
            });

        // Act
        var result = await _agent.ExecuteRemediationAsync(request);

        // Assert
        result.Status.Should().Be(ExecutionStatus.Failed);
        result.RollbackExecuted.Should().BeTrue();
        result.RollbackSuccess.Should().BeTrue();

        _mockStore.Verify(s => s.EscalateIncidentAsync(incidentId, It.Is<string>(r => r.Contains("Post-flight check failed")), It.IsAny<CancellationToken>()), Times.Once);
        _recordedEvents.Should().Contain(e => e.EventType == "postflight_check_failed");
        _recordedEvents.Should().Contain(e => e.EventType == "remediation_rollback_executed");
    }

    [Fact]
    public async Task ProcessCommandSandbox_ExecutesEchoCommandSuccessfully()
    {
        // Real process test on current OS
        var loggerMock = new Mock<ILogger<ProcessCommandSandbox>>();
        var sandbox = new ProcessCommandSandbox(loggerMock.Object);

        var output = await sandbox.ExecuteCommandAsync("echo TrinetraTest", TimeSpan.FromSeconds(5));

        output.ExitCode.Should().Be(0);
        output.StandardOutput.Should().Contain("TrinetraTest");
        output.TimedOut.Should().BeFalse();
        output.DurationMs.Should().BeGreaterThanOrEqualTo(0);
    }
}
