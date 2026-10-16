using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Trinetra.API.DTOs;
using Xunit;

namespace Trinetra.IntegrationTests;

public class IncidentEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public IncidentEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateIncident_ShouldReturn201Created_AndWriteEvent()
    {
        var request = new CreateIncidentRequest
        {
            AlertId = $"alert-{Guid.NewGuid():N}",
            Source = "prometheus",
            Title = "High CPU Utilization on Node 1",
            Description = "CPU exceeded 90% for 5 minutes",
            Severity = "critical",
            AffectedService = "billing-service"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/incidents", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<CreateIncidentResponse>();
        created.Should().NotBeNull();
        created!.IncidentId.Should().NotBeEmpty();
        created.Severity.Should().Be("critical");

        // Verify GET /api/v1/incidents/{id} includes the created incident with events
        var getDetail = await _client.GetAsync($"/api/v1/incidents/{created.IncidentId}");
        getDetail.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await getDetail.Content.ReadFromJsonAsync<IncidentDetailDto>();
        detail.Should().NotBeNull();
        detail!.Title.Should().Be("High CPU Utilization on Node 1");
        detail.Events.Should().NotBeEmpty(); // Ground rule: audit event exists
        detail.Events.First().EventType.Should().Be("incident_created");
    }

    [Fact]
    public async Task GetIncidents_ShouldReturn200OK_WithPagination()
    {
        var response = await _client.GetAsync("/api/v1/incidents?limit=10&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<IncidentListItemDto>>();
        result.Should().NotBeNull();
        result!.Limit.Should().Be(10);
    }

    [Fact]
    public async Task PatchIncident_ShouldReturn200OK_AndAppendAuditEvent()
    {
        // 1. Create incident
        var createReq = new CreateIncidentRequest
        {
            AlertId = $"alert-{Guid.NewGuid():N}",
            Source = "manual",
            Title = "Memory Leak in User Service",
            Severity = "high"
        };
        var createRes = await _client.PostAsJsonAsync("/api/v1/incidents", createReq);
        var created = await createRes.Content.ReadFromJsonAsync<CreateIncidentResponse>();

        // 2. Patch status
        var patchReq = new UpdateIncidentRequest
        {
            Status = "investigating",
            Notes = "Assigned to team"
        };
        var patchRes = await _client.PatchAsJsonAsync($"/api/v1/incidents/{created!.IncidentId}", patchReq);
        patchRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Verify GET events includes 2 events
        var eventsRes = await _client.GetAsync($"/api/v1/incidents/{created.IncidentId}/events");
        eventsRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var eventsResult = await eventsRes.Content.ReadFromJsonAsync<PagedResult<IncidentEventDto>>();
        eventsResult.Should().NotBeNull();
        eventsResult!.Items.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task WebhookAlert_PrometheusFormat_ShouldCreateIncident()
    {
        var promAlert = new
        {
            alerts = new[]
            {
                new
                {
                    status = "firing",
                    labels = new Dictionary<string, string>
                    {
                        ["alertname"] = "PodCrashLooping",
                        ["service"] = "order-api",
                        ["severity"] = "critical"
                    },
                    annotations = new Dictionary<string, string>
                    {
                        ["summary"] = "Pod restarting repeatedly"
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/webhooks/alerts", promAlert);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var webhookResponse = await response.Content.ReadFromJsonAsync<WebhookAlertResponse>();
        webhookResponse.Should().NotBeNull();
        webhookResponse!.IncidentId.Should().NotBeEmpty();
        webhookResponse.InvestigationStarting.Should().BeTrue();
    }

    [Fact]
    public async Task WebhookAlert_DatadogFormat_ShouldCreateIncident()
    {
        var ddAlert = new
        {
            alert = new
            {
                id = 554433,
                title = "High Error Rate in Checkout Service",
                metric = "trace.express.request.errors",
                current_value = 22.5,
                threshold_value = 10.0
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/webhooks/alerts", ddAlert);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var webhookResponse = await response.Content.ReadFromJsonAsync<WebhookAlertResponse>();
        webhookResponse.Should().NotBeNull();
        webhookResponse!.IncidentId.Should().NotBeEmpty();
    }
}
