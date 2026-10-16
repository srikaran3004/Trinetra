using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Trinetra.API.DTOs;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Infrastructure.Auth;
using Trinetra.Infrastructure.Data;
using Xunit;

namespace Trinetra.IntegrationTests;

public class AuthAndEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthAndEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Swagger_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/swagger/index.html");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthLogin_AndRefresh_Workflow_ShouldSucceed()
    {
        // 1. Seed user in db
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

            if (!db.Users.Any(u => u.Username == "testuser"))
            {
                db.Users.Add(new User
                {
                    Username = "testuser",
                    Email = "testuser@trinetra.local",
                    PasswordHash = hasher.HashPassword("TestPass123!"),
                    Role = UserRoles.Admin,
                    IsActive = true
                });
                await db.SaveChangesAsync();
            }
        }

        // 2. Login
        var loginReq = new LoginRequest
        {
            Username = "testuser",
            Password = "TestPass123!"
        };
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponse>();
        loginData.Should().NotBeNull();
        loginData!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginData.RefreshToken.Should().NotBeNullOrWhiteSpace();

        // 3. Refresh token
        var refreshReq = new RefreshTokenRequest
        {
            RefreshToken = loginData.RefreshToken
        };
        var refreshRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshReq);
        refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshData = await refreshRes.Content.ReadFromJsonAsync<LoginResponse>();
        refreshData.Should().NotBeNull();
        refreshData!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ApprovalsPending_ShouldReturn200OK()
    {
        var response = await _client.GetAsync("/api/v1/approvals/pending");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Runbook_CreateAndGet_ShouldSucceed()
    {
        var request = new CreateRunbookRequest
        {
            Title = "Kubernetes Pod Restart Procedure",
            Description = "Safe rolling restart of kubernetes deployment pods",
            Category = "kubernetes",
            Tags = new List<string> { "k8s", "restart", "deployment" },
            ApplicableServices = new List<string> { "payment-service" },
            Content = "## Runbook Steps\n1. kubectl rollout restart deployment",
            EstimatedTimeMinutes = 5
        };

        var response = await _client.PostAsJsonAsync("/api/v1/runbooks", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _client.GetAsync("/api/v1/runbooks?limit=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<PagedResult<RunbookListItemDto>>();
        list.Should().NotBeNull();
        list!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DashboardSummaryAndMetrics_ShouldReturn200OK()
    {
        var summaryRes = await _client.GetAsync("/api/v1/dashboard/summary");
        summaryRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await summaryRes.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        summary.Should().NotBeNull();
        summary!.AvgMttrMinutes.Should().BeGreaterThan(0);

        var metricsRes = await _client.GetAsync("/api/v1/dashboard/metrics");
        metricsRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var metrics = await metricsRes.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        metrics.Should().NotBeNull();
        metrics!.IncidentTrends.Should().NotBeNull();
        metrics.SeverityDistribution.Should().NotBeNull();
    }
}
