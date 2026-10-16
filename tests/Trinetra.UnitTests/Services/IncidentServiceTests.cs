using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Trinetra.API.DTOs;
using Trinetra.API.Hubs;
using Trinetra.API.Services;
using Trinetra.Core.Enums;
using Trinetra.Infrastructure.Data;
using Xunit;

namespace Trinetra.UnitTests.Services;

public class IncidentServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IHubContext<IncidentHub, IIncidentClient>> _mockHubContext;
    private readonly Mock<IHubClients<IIncidentClient>> _mockHubClients;
    private readonly Mock<IIncidentClient> _mockIncidentClient;
    private readonly Mock<ILogger<IncidentService>> _mockLogger;
    private readonly IncidentService _incidentService;

    public IncidentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();

        _mockHubContext = new Mock<IHubContext<IncidentHub, IIncidentClient>>();
        _mockHubClients = new Mock<IHubClients<IIncidentClient>>();
        _mockIncidentClient = new Mock<IIncidentClient>();

        _mockHubClients.Setup(c => c.All).Returns(_mockIncidentClient.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(_mockHubClients.Object);
        _mockLogger = new Mock<ILogger<IncidentService>>();

        _incidentService = new IncidentService(_dbContext, _mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task CreateIncident_MustWriteIncidentAndIncidentEvent_AuditTrailEnforced()
    {
        // Non-negotiable rule: Every write to incidents DB must also write a row to incident_events
        var request = new CreateIncidentRequest
        {
            AlertId = "alert-test-001",
            Source = "prometheus",
            Title = "API Latency High",
            Description = "P99 latency > 1000ms",
            Severity = IncidentSeverities.High,
            AffectedService = "gateway-api"
        };

        var response = await _incidentService.CreateIncidentAsync(request);

        response.Should().NotBeNull();
        response.IncidentId.Should().NotBeEmpty();

        // 1. Verify Incident exists
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == response.IncidentId);
        incident.Should().NotBeNull();
        incident!.Title.Should().Be("API Latency High");
        incident.Status.Should().Be(IncidentStatuses.Triggered);
        incident.AffectedServices.Should().Contain("gateway-api");

        // 2. Verify IncidentEvent row was created
        var events = await _dbContext.IncidentEvents.Where(e => e.IncidentId == response.IncidentId).ToListAsync();
        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("incident_created");
        events[0].ExecutionStatus.Should().Be(ExecutionStatuses.Success);
    }

    [Fact]
    public async Task UpdateIncident_MustWriteIncidentEventRow()
    {
        var createRequest = new CreateIncidentRequest
        {
            AlertId = "alert-update-001",
            Source = "manual",
            Title = "Disk Space Low",
            Severity = IncidentSeverities.Medium
        };
        var createResponse = await _incidentService.CreateIncidentAsync(createRequest);

        var updateRequest = new UpdateIncidentRequest
        {
            Status = IncidentStatuses.Investigating,
            Notes = "Assigned to on-call engineer"
        };

        var updated = await _incidentService.UpdateIncidentAsync(createResponse.IncidentId, updateRequest);
        updated.Should().BeTrue();

        var incident = await _dbContext.Incidents.FindAsync(createResponse.IncidentId);
        incident!.Status.Should().Be(IncidentStatuses.Investigating);
        incident.StartedInvestigatingAt.Should().NotBeNull();

        // Verify two events now exist: created and updated
        var events = await _dbContext.IncidentEvents.Where(e => e.IncidentId == createResponse.IncidentId).ToListAsync();
        events.Should().HaveCount(2);
        events.Should().Contain(e => e.EventType == "incident_updated");
    }

    [Fact]
    public async Task GetIncidents_WithStatusAndSeverityFilter_ReturnsFilteredResults()
    {
        await _incidentService.CreateIncidentAsync(new CreateIncidentRequest
        {
            AlertId = "a1",
            Title = "Inc 1",
            Severity = IncidentSeverities.Critical,
            Source = "prometheus"
        });
        await _incidentService.CreateIncidentAsync(new CreateIncidentRequest
        {
            AlertId = "a2",
            Title = "Inc 2",
            Severity = IncidentSeverities.Low,
            Source = "datadog"
        });

        var result = await _incidentService.GetIncidentsAsync(status: null, severity: "critical", limit: 10, offset: 0, sortBy: null);

        result.Items.Should().HaveCount(1);
        result.Items.First().Severity.Should().Be("critical");
    }

    [Fact]
    public async Task ProcessPrometheusAlert_CreatesIncidentAndAuditTrail()
    {
        var payload = new PrometheusAlertPayload
        {
            Alerts = new List<PrometheusAlertItem>
            {
                new()
                {
                    Status = "firing",
                    Labels = new Dictionary<string, string>
                    {
                        ["alertname"] = "DatabaseHighConnections",
                        ["service"] = "postgres",
                        ["severity"] = "critical"
                    },
                    Annotations = new Dictionary<string, string>
                    {
                        ["description"] = "Connections > 90%"
                    }
                }
            }
        };

        var response = await _incidentService.ProcessPrometheusAlertAsync(payload);

        response.Should().NotBeNull();
        response.InvestigationStarting.Should().BeTrue();

        var incident = await _dbContext.Incidents.FindAsync(response.IncidentId);
        incident.Should().NotBeNull();
        incident!.Severity.Should().Be("critical");
        incident.AffectedServices.Should().Contain("postgres");

        var events = await _dbContext.IncidentEvents.Where(e => e.IncidentId == response.IncidentId).ToListAsync();
        events.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessDatadogAlert_CreatesIncidentAndAuditTrail()
    {
        var payload = new DatadogAlertPayload
        {
            Alert = new DatadogAlertItem
            {
                Id = 98765,
                Title = "High CPU on Kubernetes Node",
                Metric = "kubernetes.cpu.usage",
                CurrentValue = 95.5m,
                ThresholdValue = 85.0m
            }
        };

        var response = await _incidentService.ProcessDatadogAlertAsync(payload);

        response.Should().NotBeNull();
        response.IncidentId.Should().NotBeEmpty();

        var incident = await _dbContext.Incidents.FindAsync(response.IncidentId);
        incident.Should().NotBeNull();
        incident!.AlertId.Should().Be("dd-98765");

        var events = await _dbContext.IncidentEvents.Where(e => e.IncidentId == response.IncidentId).ToListAsync();
        events.Should().HaveCount(1);
    }
}
