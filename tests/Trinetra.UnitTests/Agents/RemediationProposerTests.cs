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
using Trinetra.Agents.RemediationProposer;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Analysis;
using Trinetra.Core.Models.Remediation;
using Xunit;

namespace Trinetra.UnitTests.Agents;

public class RemediationProposerTests
{
    private readonly Mock<IApprovalPolicyRepository> _mockPolicyRepo;
    private readonly Mock<IIncidentCoordinatorStore> _mockStore;
    private readonly Mock<IAuditEventWriter> _mockAuditWriter;
    private readonly Mock<ILogger<RemediationProposerPlugin>> _mockPluginLogger;
    private readonly Mock<ILogger<RemediationProposerAgent>> _mockAgentLogger;
    private readonly RemediationProposerPlugin _plugin;
    private readonly RemediationProposerAgent _agent;
    private readonly List<IncidentEvent> _recordedEvents = new();

    public RemediationProposerTests()
    {
        _mockPolicyRepo = new Mock<IApprovalPolicyRepository>();
        _mockStore = new Mock<IIncidentCoordinatorStore>();
        _mockAuditWriter = new Mock<IAuditEventWriter>();
        _mockPluginLogger = new Mock<ILogger<RemediationProposerPlugin>>();
        _mockAgentLogger = new Mock<ILogger<RemediationProposerAgent>>();

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

        // Setup default matching policy mock
        _mockPolicyRepo.Setup(p => p.FindMatchingPolicyAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? cmd, string? svc, string? risk, string? sev, CancellationToken _) =>
            {
                if (string.Equals(risk, RemediationRiskLevels.High, StringComparison.OrdinalIgnoreCase))
                {
                    return new ApprovalPolicy
                    {
                        RuleName = "high_risk_remediation_approval",
                        RequiresApproval = true,
                        ApprovalLevel = "lead",
                        EscalationTimeoutMinutes = 15
                    };
                }
                if (string.Equals(risk, RemediationRiskLevels.Medium, StringComparison.OrdinalIgnoreCase))
                {
                    return new ApprovalPolicy
                    {
                        RuleName = "medium_risk_deployment_rollback",
                        RequiresApproval = true,
                        ApprovalLevel = "sre",
                        EscalationTimeoutMinutes = 15
                    };
                }
                return new ApprovalPolicy
                {
                    RuleName = "low_risk_auto_approved",
                    RequiresApproval = false,
                    ApprovalLevel = null
                };
            });

        _plugin = new RemediationProposerPlugin(_mockPolicyRepo.Object, _mockPluginLogger.Object);

        var config = new AgentConfiguration
        {
            ModelId = "claude-sonnet-4-5",
            Temperature = 0.2,
            MaxTokens = 3000
        };

        _agent = new RemediationProposerAgent(
            config,
            new Kernel(),
            _mockAgentLogger.Object,
            _plugin,
            _mockStore.Object,
            _mockAuditWriter.Object);
    }

    [Fact]
    public void GenerateRemediationOptions_ForDeployment_ShouldGenerateStructuredOptionsWithRollback()
    {
        var optionsJson = _plugin.GenerateRemediationOptions(
            RootCauseCategories.Deployment,
            "Database connection pool exhaustion caused by unoptimized query in auth-api v2.3.4",
            serviceName: "auth-api");

        optionsJson.Should().NotBeNullOrWhiteSpace();
        var options = JsonSerializer.Deserialize<List<RemediationOption>>(optionsJson);

        options.Should().NotBeNull();
        options!.Count.Should().BeGreaterOrEqualTo(2);

        var rollbackOpt = options.First(o => o.ActionCommand == "deployment_rollback");
        rollbackOpt.Title.Should().Contain("Rollback");
        rollbackOpt.Recommended.Should().BeTrue();
        rollbackOpt.RiskLevel.Should().Be(RemediationRiskLevels.Medium);
        rollbackOpt.SuccessProbability.Should().BeGreaterThan(0.90);
        rollbackOpt.PreflightChecks.Should().NotBeEmpty();
        rollbackOpt.PostflightChecks.Should().NotBeEmpty();
    }

    [Fact]
    public void GenerateRemediationOptions_ForDatabase_ShouldGenerateKillQueriesAndFailover()
    {
        var optionsJson = _plugin.GenerateRemediationOptions(
            RootCauseCategories.Database,
            "Database connection pool exhaustion or query lock contention",
            serviceName: "payment-db");

        var options = JsonSerializer.Deserialize<List<RemediationOption>>(optionsJson);

        options.Should().NotBeNull();
        options!.Should().Contain(o => o.ActionCommand == "kill_idle_queries");
        options!.Should().Contain(o => o.ActionCommand == "scale_db_pool");
        options!.Should().Contain(o => o.ActionCommand == "database_failover");

        var failover = options.First(o => o.ActionCommand == "database_failover");
        failover.RiskLevel.Should().Be(RemediationRiskLevels.High);
        failover.RequiresApproval.Should().BeTrue();
    }

    [Fact]
    public void AssessRemediationRisk_ShouldCorrectlyCategorizeCommands()
    {
        // High risk
        var highRiskJson = _plugin.AssessRemediationRisk("database_failover", "Promote standby replica");
        using (var doc = JsonDocument.Parse(highRiskJson))
        {
            doc.RootElement.GetProperty("risk_level").GetString().Should().Be(RemediationRiskLevels.High);
        }

        // Medium risk
        var mediumRiskJson = _plugin.AssessRemediationRisk("deployment_rollback", "Roll back pods to v1.2");
        using (var doc = JsonDocument.Parse(mediumRiskJson))
        {
            doc.RootElement.GetProperty("risk_level").GetString().Should().Be(RemediationRiskLevels.Medium);
        }

        // Low risk
        var lowRiskJson = _plugin.AssessRemediationRisk("scale_up_pods", "Scale replicas from 2 to 5");
        using (var doc = JsonDocument.Parse(lowRiskJson))
        {
            doc.RootElement.GetProperty("risk_level").GetString().Should().Be(RemediationRiskLevels.Low);
        }
    }

    [Fact]
    public async Task EvaluateApprovalPolicy_ShouldEnforceApprovalForMediumAndHighRisk()
    {
        // High risk
        var highJson = await _plugin.EvaluateApprovalPolicyAsync("database_failover", "auth-api", RemediationRiskLevels.High, "critical");
        using (var doc = JsonDocument.Parse(highJson))
        {
            doc.RootElement.GetProperty("requires_approval").GetBoolean().Should().BeTrue();
            doc.RootElement.GetProperty("approval_level").GetString().Should().Be("lead");
        }

        // Medium risk
        var medJson = await _plugin.EvaluateApprovalPolicyAsync("deployment_rollback", "auth-api", RemediationRiskLevels.Medium, "high");
        using (var doc = JsonDocument.Parse(medJson))
        {
            doc.RootElement.GetProperty("requires_approval").GetBoolean().Should().BeTrue();
            doc.RootElement.GetProperty("approval_level").GetString().Should().Be("sre");
        }

        // Low risk
        var lowJson = await _plugin.EvaluateApprovalPolicyAsync("scale_up_pods", "auth-api", RemediationRiskLevels.Low, "medium");
        using (var doc = JsonDocument.Parse(lowJson))
        {
            doc.RootElement.GetProperty("requires_approval").GetBoolean().Should().BeFalse();
        }
    }

    [Fact]
    public async Task ProposeRemediationAsync_ShouldEnforceAuditTrailAndPersistFindings()
    {
        var incidentId = Guid.NewGuid();
        var rootCause = "Database connection pool exhaustion caused by unoptimized query in auth-api v2.3.4 deployment";

        var report = await _agent.ProposeRemediationAsync(
            incidentId,
            rootCause,
            rootCauseCategory: RootCauseCategories.Deployment,
            severity: "high",
            serviceName: "auth-api");

        report.Should().NotBeNull();
        report.IncidentId.Should().Be(incidentId);
        report.RemediationOptions.Should().HaveCountGreaterOrEqualTo(2);
        report.RecommendedOption.Should().NotBeNull();
        report.RecommendedOption!.Title.Should().Contain("Rollback");

        // Verify findings persisted to database
        _mockStore.Verify(s => s.UpdateIncidentFindingsAsync(
            incidentId,
            null,
            null,
            null,
            It.Is<string?>(p => p != null && p.Contains("Rollback")),
            It.Is<string?>(r => r == RemediationRiskLevels.Medium),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify Ground Rule: Every agent action writes to incident_events
        _recordedEvents.Should().HaveCountGreaterOrEqualTo(2);
        _recordedEvents.Should().Contain(e => e.EventType == "remediation_proposal_started" && e.IncidentId == incidentId);
        _recordedEvents.Should().Contain(e => e.EventType == "remediation_proposal_completed" && e.IncidentId == incidentId);

        var completedEvent = _recordedEvents.First(e => e.EventType == "remediation_proposal_completed");
        completedEvent.ActionDescription.Should().Contain("Remediation proposal generated");
        completedEvent.AgentName.Should().Be("remediation_proposer");
        completedEvent.RequiresApproval.Should().BeTrue();
    }
}
