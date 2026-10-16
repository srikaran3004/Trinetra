using FluentAssertions;
using Pgvector;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Xunit;

namespace Trinetra.UnitTests.Entities;

public class EntityConstraintTests
{
    [Fact]
    public void Incident_ShouldInitialize_WithDefaultValues()
    {
        // Act
        var incident = new Incident
        {
            AlertId = "alert-101",
            Source = "prometheus",
            Title = "High CPU",
            Severity = IncidentSeverities.High
        };

        // Assert
        incident.Id.Should().NotBeEmpty();
        incident.Status.Should().Be(IncidentStatuses.Triggered);
        incident.Severity.Should().Be("high");
        incident.AffectedServices.Should().NotBeNull().And.BeEmpty();
        incident.AffectedHosts.Should().NotBeNull().And.BeEmpty();
        incident.AffectedComponents.Should().NotBeNull().And.BeEmpty();
        incident.Events.Should().NotBeNull().And.BeEmpty();
        incident.LogsSnapshots.Should().NotBeNull().And.BeEmpty();
        incident.MetricsSnapshots.Should().NotBeNull().And.BeEmpty();
        incident.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Runbook_ContentEmbedding_ShouldBeNullable_PerDecisionD11()
    {
        // Decision D-11: ContentEmbedding is nullable because embeddings are generated async post-creation
        var runbook = new Runbook
        {
            Title = "Restart Pod Runbook",
            Content = "# Steps\n1. kubectl delete pod",
            ContentEmbedding = null // explicitly null
        };

        runbook.ContentEmbedding.Should().BeNull();
        runbook.IsActive.Should().BeTrue();
        runbook.Version.Should().Be(1);
    }

    [Fact]
    public void User_ApiKeyHash_ShouldBeNullable_PerDecisionD12()
    {
        // Decision D-12: Users table has no check constraint requiring api_key_hash
        var user = new User
        {
            Username = "sre_engineer",
            Email = "sre@trinetra.local",
            PasswordHash = "hashed_pw",
            Role = UserRoles.Sre,
            ApiKeyHash = null // allowed to be null initially
        };

        user.ApiKeyHash.Should().BeNull();
        user.IsActive.Should().BeTrue();
        user.Role.Should().Be("sre");
    }

    [Fact]
    public void IncidentEvent_ShouldInitialize_WithDefaults()
    {
        var incidentId = Guid.NewGuid();
        var evt = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "analysis_started",
            AgentName = "IncidentCoordinatorAgent",
            ActionDescription = "Investigation began"
        };

        evt.Id.Should().NotBeEmpty();
        evt.IncidentId.Should().Be(incidentId);
        evt.ExecutionStatus.Should().Be(ExecutionStatuses.Pending);
        evt.GuardrailTriggered.Should().BeFalse();
        evt.RequiresApproval.Should().BeFalse();
        evt.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ApprovalPolicy_ShouldInitialize_WithDefaults()
    {
        var policy = new ApprovalPolicy
        {
            RuleName = "test_policy",
            Priority = 50,
            MinRiskLevel = RiskLevels.Medium,
            RequiresApproval = true,
            ApprovalLevel = "lead"
        };

        policy.Id.Should().NotBeEmpty();
        policy.IsActive.Should().BeTrue();
        policy.EscalationTimeoutMinutes.Should().Be(15);
        policy.MaxConcurrentExecutions.Should().Be(1);
        policy.MaxPerHour.Should().Be(10);
    }

    [Theory]
    [InlineData("critical")]
    [InlineData("high")]
    [InlineData("medium")]
    [InlineData("low")]
    public void IncidentSeverities_ShouldContainValidValues(string severity)
    {
        IncidentSeverities.All.Should().Contain(severity);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("lead")]
    [InlineData("manager")]
    [InlineData("sre")]
    [InlineData("viewer")]
    public void UserRoles_ShouldContainValidRoles(string role)
    {
        UserRoles.All.Should().Contain(role);
    }
}
