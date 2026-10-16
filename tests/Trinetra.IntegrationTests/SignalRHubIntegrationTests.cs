using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Trinetra.API.DTOs;
using Trinetra.API.Hubs;
using Trinetra.API.Services;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.IntegrationTests;

public class SignalRHubIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SignalRHubIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SignalRIncidentEventNotifier_ShouldDispatchEventsToHubContext()
    {
        var mockHubContext = new Mock<IHubContext<IncidentHub, IIncidentClient>>();
        var mockClients = new Mock<IHubClients<IIncidentClient>>();
        var mockClient = new Mock<IIncidentClient>();
        var mockLogger = new Mock<ILogger<SignalRIncidentEventNotifier>>();

        mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.All).Returns(mockClient.Object);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClient.Object);

        var notifier = new SignalRIncidentEventNotifier(mockHubContext.Object, mockLogger.Object);
        var incidentId = Guid.NewGuid();

        // 1. Notify incident created
        await notifier.NotifyIncidentCreatedAsync(incidentId, "High Latency", "P1", "triggered", DateTime.UtcNow);
        mockClient.Verify(c => c.IncidentCreated(It.IsAny<object>()), Times.Once);

        // 2. Notify status changed
        await notifier.NotifyIncidentStatusChangedAsync(incidentId, "investigating", DateTimeOffset.UtcNow);
        mockClient.Verify(c => c.IncidentStatusChanged(It.IsAny<object>()), Times.AtLeastOnce);

        // 3. Notify agent action completed
        await notifier.NotifyAgentActionCompletedAsync(incidentId, "log_analyzer", "Parsed logs", "query_logs", "{}", "success");
        mockClient.Verify(c => c.AgentActionCompleted(It.IsAny<object>()), Times.AtLeastOnce);

        // 4. Notify approval required
        await notifier.NotifyApprovalRequiredAsync(incidentId, Guid.NewGuid(), "Restart pods", "kubectl restart", 30);
        mockClient.Verify(c => c.ApprovalRequired(It.IsAny<object>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DbAuditEventWriter_WhenEventRequiresApproval_WritesToDbAndEmitsNotification()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<DbAuditEventWriter>>();
        var mockNotifier = new Mock<IIncidentEventNotifier>();

        var writer = new DbAuditEventWriter(dbContext, mockLogger.Object, mockNotifier.Object);

        var incidentId = Guid.NewGuid();
        var incidentEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            AgentName = "auto_executor",
            EventType = "approval_required",
            ActionDescription = "Restarting deployment requires human approval",
            RequiresApproval = true,
            ApprovalTimeoutMinutes = 15,
            ExecutionStatus = "pending"
        };

        var savedEvent = await writer.WriteEventAsync(incidentEvent);

        // Verify DB write
        savedEvent.Should().NotBeNull();
        var dbEvent = await dbContext.IncidentEvents.FirstOrDefaultAsync(e => e.Id == savedEvent.Id);
        dbEvent.Should().NotBeNull();
        dbEvent!.RequiresApproval.Should().BeTrue();

        // Verify real-time notification emitted
        mockNotifier.Verify(n => n.NotifyApprovalRequiredAsync(
            incidentId,
            savedEvent.Id,
            savedEvent.ActionDescription!,
            savedEvent.ToolCall,
            15,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DbAuditEventWriter_WhenAgentActionCompleted_WritesToDbAndEmitsNotification()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        var mockLogger = new Mock<ILogger<DbAuditEventWriter>>();
        var mockNotifier = new Mock<IIncidentEventNotifier>();

        var writer = new DbAuditEventWriter(dbContext, mockLogger.Object, mockNotifier.Object);

        var incidentId = Guid.NewGuid();
        var incidentEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            AgentName = "metrics_querier",
            EventType = "metrics_analysis_completed",
            ActionDescription = "Completed metrics analysis; detected CPU saturation at 98%",
            ToolResult = "{\"cpu\": 0.98}",
            ExecutionStatus = "success"
        };

        var savedEvent = await writer.WriteEventAsync(incidentEvent);

        // Verify DB write
        savedEvent.Should().NotBeNull();
        var dbEvent = await dbContext.IncidentEvents.FirstOrDefaultAsync(e => e.Id == savedEvent.Id);
        dbEvent.Should().NotBeNull();
        dbEvent!.AgentName.Should().Be("metrics_querier");

        // Verify real-time notification emitted
        mockNotifier.Verify(n => n.NotifyAgentActionCompletedAsync(
            incidentId,
            "metrics_querier",
            savedEvent.ActionDescription!,
            savedEvent.ToolCall,
            savedEvent.ToolResult,
            "success",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IncidentHub_JoinAndLeaveGroup_ExecutesWithoutError()
    {
        var hub = new IncidentHub();
        var mockClients = new Mock<IHubCallerClients<IIncidentClient>>();
        var mockGroups = new Mock<IGroupManager>();
        var mockContext = new Mock<HubCallerContext>();

        mockContext.Setup(c => c.ConnectionId).Returns("conn-123");
        mockGroups.Setup(g => g.AddToGroupAsync("conn-123", "incident_test-inc", default))
            .Returns(Task.CompletedTask);
        mockGroups.Setup(g => g.RemoveFromGroupAsync("conn-123", "incident_test-inc", default))
            .Returns(Task.CompletedTask);

        hub.Context = mockContext.Object;
        hub.Groups = mockGroups.Object;
        hub.Clients = mockClients.Object;

        await hub.JoinIncidentGroup("test-inc");
        mockGroups.Verify(g => g.AddToGroupAsync("conn-123", "incident_test-inc", default), Times.Once);

        await hub.LeaveIncidentGroup("test-inc");
        mockGroups.Verify(g => g.RemoveFromGroupAsync("conn-123", "incident_test-inc", default), Times.Once);
    }
}
