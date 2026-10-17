using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.UnitTests.Services;

public class ApprovalWorkflowTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IIncidentEventNotifier> _mockNotifier;
    private readonly Mock<ILogger<ApprovalWorkflowService>> _mockLogger;
    private readonly ApprovalWorkflowService _service;

    public ApprovalWorkflowTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TrinetraApprovalTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mockNotifier = new Mock<IIncidentEventNotifier>();
        _mockLogger = new Mock<ILogger<ApprovalWorkflowService>>();

        _service = new ApprovalWorkflowService(_dbContext, _mockLogger.Object, _mockNotifier.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateApprovalRequestAsync_ShouldTransitionToAwaitingApproval_AndDispatchSignalR()
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = "Database connection exhaustion",
            Status = IncidentStatuses.AnalysisComplete,
            Severity = IncidentSeverities.High
        };

        _dbContext.Incidents.Add(incident);
        await _dbContext.SaveChangesAsync();

        var evt = await _service.CreateApprovalRequestAsync(
            incident.Id,
            actionDescription: "Terminate idle blocking database queries",
            toolCall: "kill_idle_queries",
            riskLevel: "medium",
            approvalLevel: "lead",
            timeoutMinutes: 15);

        evt.Should().NotBeNull();
        evt.IncidentId.Should().Be(incident.Id);
        evt.RequiresApproval.Should().BeTrue();
        evt.ExecutionStatus.Should().Be(ExecutionStatuses.Pending);
        evt.EventType.Should().Be("approval_required");

        // Verify incident status transitioned
        var updatedIncident = await _dbContext.Incidents.FindAsync(incident.Id);
        updatedIncident!.Status.Should().Be(IncidentStatuses.AwaitingApproval);

        // Verify SignalR notifications
        _mockNotifier.Verify(n => n.NotifyApprovalRequiredAsync(
            incident.Id,
            evt.Id,
            "Terminate idle blocking database queries",
            "kill_idle_queries",
            15,
            It.IsAny<CancellationToken>()), Times.Once);

        _mockNotifier.Verify(n => n.NotifyIncidentStatusChangedAsync(
            incident.Id,
            IncidentStatuses.AwaitingApproval,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_ShouldTransitionIncidentToExecuting_AndRecordAuditTrail()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Payment API Regression",
            Status = IncidentStatuses.AwaitingApproval,
            Severity = IncidentSeverities.High
        };

        var approvalEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "approval_required",
            ActionDescription = "Rollback auth-api to v2.3.3",
            ToolCall = "deployment_rollback",
            RequiresApproval = true,
            ExecutionStatus = ExecutionStatuses.Pending
        };

        _dbContext.Incidents.Add(incident);
        _dbContext.IncidentEvents.Add(approvalEvent);
        await _dbContext.SaveChangesAsync();

        var approverUserId = Guid.NewGuid();
        var success = await _service.ApproveAsync(
            approvalEvent.Id,
            approverUserId,
            notes: "Approved after verifying replication lag");

        success.Should().BeTrue();

        // Verify approval event updated
        var updatedEvent = await _dbContext.IncidentEvents.FindAsync(approvalEvent.Id);
        updatedEvent!.ApprovedBy.Should().Be(approverUserId);
        updatedEvent.ApprovalTimestamp.Should().NotBeNull();
        updatedEvent.ExecutionStatus.Should().Be(ExecutionStatuses.Success);

        // Verify incident transitioned to Executing
        var updatedIncident = await _dbContext.Incidents.FindAsync(incidentId);
        updatedIncident!.Status.Should().Be(IncidentStatuses.Executing);
        updatedIncident.RemediationStartedAt.Should().NotBeNull();

        // Verify audit event written
        var auditEvents = await _dbContext.IncidentEvents
            .Where(e => e.IncidentId == incidentId && e.EventType == "remediation_approved")
            .ToListAsync();

        auditEvents.Should().HaveCount(1);
        auditEvents[0].ActionDescription.Should().Contain("Rollback auth-api");
        auditEvents[0].AgentName.Should().Be("ApprovalGate");

        // Verify SignalR broadcast
        _mockNotifier.Verify(n => n.NotifyIncidentStatusChangedAsync(
            incidentId,
            IncidentStatuses.Executing,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DenyAsync_ShouldEscalateIncident_AndRecordAuditTrail()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Database primary saturation",
            Status = IncidentStatuses.AwaitingApproval,
            Severity = IncidentSeverities.Critical
        };

        var approvalEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "approval_required",
            ActionDescription = "Failover to secondary database replica",
            ToolCall = "database_failover",
            RequiresApproval = true,
            ExecutionStatus = ExecutionStatuses.Pending
        };

        _dbContext.Incidents.Add(incident);
        _dbContext.IncidentEvents.Add(approvalEvent);
        await _dbContext.SaveChangesAsync();

        var denierUserId = Guid.NewGuid();
        var success = await _service.DenyAsync(
            approvalEvent.Id,
            denierUserId,
            reason: "Replica lag too high, risk of data loss");

        success.Should().BeTrue();

        // Verify approval event updated
        var updatedEvent = await _dbContext.IncidentEvents.FindAsync(approvalEvent.Id);
        updatedEvent!.ApprovalNotes.Should().Contain("Replica lag too high");
        updatedEvent.ExecutionStatus.Should().Be(ExecutionStatuses.Failed);

        // Verify incident transitioned to Escalated
        var updatedIncident = await _dbContext.Incidents.FindAsync(incidentId);
        updatedIncident!.Status.Should().Be(IncidentStatuses.Escalated);
        updatedIncident.EscalatedAt.Should().NotBeNull();

        // Verify audit event written
        var auditEvents = await _dbContext.IncidentEvents
            .Where(e => e.IncidentId == incidentId && e.EventType == "remediation_denied")
            .ToListAsync();

        auditEvents.Should().HaveCount(1);
        auditEvents[0].ActionDescription.Should().Contain("Replica lag too high");

        // Verify SignalR broadcast
        _mockNotifier.Verify(n => n.NotifyIncidentStatusChangedAsync(
            incidentId,
            IncidentStatuses.Escalated,
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveByIncidentIdAsync_ShouldFindPendingApprovalAndApprove()
    {
        var incidentId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = incidentId,
            Title = "Incident Awaiting Approval",
            Status = IncidentStatuses.AwaitingApproval
        };

        var approvalEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "approval_required",
            ActionDescription = "Scale up replicas to 8",
            RequiresApproval = true,
            ExecutionStatus = ExecutionStatuses.Pending
        };

        _dbContext.Incidents.Add(incident);
        _dbContext.IncidentEvents.Add(approvalEvent);
        await _dbContext.SaveChangesAsync();

        var success = await _service.ApproveByIncidentIdAsync(incidentId, Guid.NewGuid(), "Approved via incident endpoint");
        success.Should().BeTrue();

        var updatedIncident = await _dbContext.Incidents.FindAsync(incidentId);
        updatedIncident!.Status.Should().Be(IncidentStatuses.Executing);
    }
}
