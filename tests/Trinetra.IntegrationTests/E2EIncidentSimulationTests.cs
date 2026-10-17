using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Trinetra.Agents.AutoExecutor;
using Trinetra.Agents.Coordinator;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.PostMortem;
using Trinetra.Agents.RemediationProposer;
using Trinetra.Agents.RootCauseAnalyzer;
using Trinetra.API.DTOs;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Execution;
using Trinetra.Infrastructure.Auth;
using Trinetra.Infrastructure.Data;
using Xunit;

namespace Trinetra.IntegrationTests;

public class E2EIncidentSimulationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public E2EIncidentSimulationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task FullIncidentLifecycle_AlertToAutoRemediationAndPostMortem_SucceedsWithFullAuditTrail()
    {
        // ── STEP 1: Alert Ingestion via Webhook ──────────────────────────────
        var alertRequest = new CreateIncidentRequest
        {
            AlertId = $"alert-db-saturation-{Guid.NewGuid():N}",
            Source = "prometheus",
            Title = "PostgreSQL Connection Pool Exhausted on orders-db",
            Description = "Active connections reached 490/500 (98%) on host orders-db-01. Error rate spiking.",
            Severity = "critical",
            AffectedService = "orders-service"
        };

        var webhookResponse = await _client.PostAsJsonAsync("/api/v1/webhooks/alerts", alertRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var alertResult = await webhookResponse.Content.ReadFromJsonAsync<WebhookAlertResponse>();
        alertResult.Should().NotBeNull();
        var incidentId = alertResult!.IncidentId;
        incidentId.Should().NotBeEmpty();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var initialIncident = await db.Incidents.FindAsync(incidentId);
            initialIncident.Should().NotBeNull();
            initialIncident!.Status.Should().Be(IncidentStatuses.Triggered);
        }

        // ── STEP 2: Parallel Investigation & Fan-Out ────────────────────────
        using (var scope = _factory.Services.CreateScope())
        {
            var coordinator = scope.ServiceProvider.GetRequiredService<IncidentCoordinatorAgent>();
            var invoker = scope.ServiceProvider.GetRequiredService<IAgentInvoker>();

            // Run parallel investigation
            var coordResult = await coordinator.CoordinateInvestigationAsync(incidentId);
            coordResult.Success.Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var investigatedIncident = await db.Incidents.FindAsync(incidentId);
            investigatedIncident!.Status.Should().Be(IncidentStatuses.AnalysisComplete);
        }

        // ── STEP 3: Root Cause Analysis ─────────────────────────────────────
        using (var scope = _factory.Services.CreateScope())
        {
            var rcaAgent = scope.ServiceProvider.GetRequiredService<RootCauseAnalyzerAgent>();
            var rcaReport = await rcaAgent.AnalyzeRootCauseAsync(
                incidentId,
                incidentSummary: "PostgreSQL Connection Pool Exhausted on orders-db");

            rcaReport.Should().NotBeNull();
            rcaReport.RootCauseCategory.Should().NotBeNullOrWhiteSpace();
            rcaReport.ConfidenceScore.Should().BeGreaterThanOrEqualTo(0.5m);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var rcaIncident = await db.Incidents.FindAsync(incidentId);
            rcaIncident!.RootCause.Should().NotBeNull();
            rcaIncident.RootCauseCategory.Should().NotBeNull();
        }

        // ── STEP 4: Remediation Planning ────────────────────────────────────
        using (var scope = _factory.Services.CreateScope())
        {
            var proposerAgent = scope.ServiceProvider.GetRequiredService<RemediationProposerAgent>();
            var proposal = await proposerAgent.ProposeRemediationAsync(
                incidentId,
                rootCause: "Unindexed query causing connection pool starvation",
                rootCauseCategory: "DATABASE",
                severity: "critical",
                serviceName: "orders-service");

            proposal.Should().NotBeNull();
            proposal.RemediationOptions.Should().NotBeEmpty();
            proposal.RecommendedOption.Should().NotBeNull();
        }

        // ── STEP 5: Safe Sandboxed Remediation Execution ────────────────────
        using (var scope = _factory.Services.CreateScope())
        {
            var autoExecutor = scope.ServiceProvider.GetRequiredService<AutoExecutorAgent>();
            var execReq = new CommandExecutionRequest
            {
                IncidentId = incidentId,
                ActionTitle = "Safe Connection Pool Recovery",
                Command = "echo Connection pool cleared and normalized",
                PreflightChecks = new List<string> { "verify database accessible" },
                PostflightChecks = new List<string> { "verify pool usage below threshold" },
                RiskLevel = "low",
                IsApproved = true
            };

            var execResult = await autoExecutor.ExecuteRemediationAsync(execReq);
            execResult.Status.Should().Be(ExecutionStatus.Success);
            execResult.ExecutionSteps.Should().HaveCount(1);
            execResult.ExecutionSteps[0].StandardOutput.Should().Contain("Connection pool cleared");

            // Incident must now be Resolved
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var resolvedIncident = await db.Incidents.FindAsync(incidentId);
            resolvedIncident!.Status.Should().Be(IncidentStatuses.Resolved);
        }

        // ── STEP 6: Post-Mortem Report Generation & History Indexing ────────
        using (var scope = _factory.Services.CreateScope())
        {
            var postMortemAgent = scope.ServiceProvider.GetRequiredService<PostMortemAgent>();
            var postMortem = await postMortemAgent.GeneratePostMortemAsync(incidentId);

            postMortem.Should().NotBeNull();
            postMortem.Title.Should().Contain("PostgreSQL");
            postMortem.MarkdownReport.Should().Contain("# Incident Post-Mortem:");
            postMortem.MarkdownReport.Should().Contain("## 1. Executive Summary");
            postMortem.MarkdownReport.Should().Contain("## 4. Incident Timeline");
            postMortem.ActionItems.Should().NotBeEmpty();

            // Verify record exists in incident_histories table
            var historyRepo = scope.ServiceProvider.GetRequiredService<IIncidentHistoryRepository>();
            var historyRecord = await historyRepo.GetByOriginalIncidentIdAsync(incidentId);
            historyRecord.Should().NotBeNull();
            historyRecord!.OriginalIncidentId.Should().Be(incidentId);
            historyRecord.RootCause.Should().NotBeNull();
        }

        // ── STEP 7: Non-Negotiable Ground Rule: Audit Trail Completeness ─────
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var events = db.IncidentEvents
                .Where(e => e.IncidentId == incidentId)
                .OrderBy(e => e.CreatedAt)
                .ToList();

            events.Should().NotBeEmpty();

            // Must include events from all phases of the lifecycle
            events.Should().Contain(e => e.EventType == "incident_created");
            events.Should().Contain(e => e.EventType == "remediation_execution_completed");
            events.Should().Contain(e => e.EventType == "post_mortem_generation_completed");
        }
    }

    [Fact]
    public async Task GuardrailEnforcement_DestructiveCommand_IsBlockedAndIncidentEscalated()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IIncidentCoordinatorStore>();
            await store.CreateIncidentAsync(new Incident
            {
                Id = incidentId,
                Title = "Test Guardrail Incident",
                Severity = "high",
                Status = IncidentStatuses.Triggered
            });
        }

        // Act: Attempt destructive rm -rf / command
        using (var scope = _factory.Services.CreateScope())
        {
            var autoExecutor = scope.ServiceProvider.GetRequiredService<AutoExecutorAgent>();
            var destructiveReq = new CommandExecutionRequest
            {
                IncidentId = incidentId,
                ActionTitle = "Destructive cleanup attempt",
                Command = "rm -rf /",
                RiskLevel = "low",
                IsApproved = true
            };

            var result = await autoExecutor.ExecuteRemediationAsync(destructiveReq);

            // Assert
            result.Status.Should().Be(ExecutionStatus.Blocked);
            result.ErrorMessage.Should().Contain("Blocked by guardrail");

            // Incident must be marked Escalated
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var incident = await db.Incidents.FindAsync(incidentId);
            incident!.Status.Should().Be(IncidentStatuses.Escalated);

            // Audit trail must record guardrail_violation
            var events = db.IncidentEvents.Where(e => e.IncidentId == incidentId).ToList();
            events.Should().Contain(e => e.EventType == "guardrail_violation");
        }
    }

    [Fact]
    public async Task HumanApprovalGate_RequiresApproval_TransitionsToAwaitingApproval_ThenExecutesUponApproval()
    {
        // 1. Create incident
        var incidentId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<IIncidentCoordinatorStore>();
            await store.CreateIncidentAsync(new Incident
            {
                Id = incidentId,
                Title = "High Risk Database Failover Incident",
                Severity = "critical",
                Status = IncidentStatuses.AnalysisComplete
            });
        }

        // 2. Request execution of command requiring approval without pre-approval
        using (var scope = _factory.Services.CreateScope())
        {
            var autoExecutor = scope.ServiceProvider.GetRequiredService<AutoExecutorAgent>();
            var req = new CommandExecutionRequest
            {
                IncidentId = incidentId,
                ActionTitle = "Scale deployment",
                Command = "kubectl scale deployment orders-api --replicas=5",
                RiskLevel = "medium",
                IsApproved = false
            };

            var blockedResult = await autoExecutor.ExecuteRemediationAsync(req);
            blockedResult.Status.Should().Be(ExecutionStatus.Blocked);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var incident = await db.Incidents.FindAsync(incidentId);
            incident!.Status.Should().Be(IncidentStatuses.AwaitingApproval);
        }

        // 3. User approves incident via approval endpoint with valid role token
        using var requestMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/incidents/{incidentId}/approve");
        requestMsg.Content = JsonContent.Create(new
        {
            Notes = "Approved by SRE Lead"
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var tokenGen = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
            var token = tokenGen.GenerateAccessToken(new User
            {
                Id = Guid.NewGuid(),
                Username = "sre-lead",
                Role = UserRoles.Lead,
                IsActive = true
            }).Token;
            requestMsg.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        var approveRes = await _client.SendAsync(requestMsg);
        approveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Incident status is now Executing
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var incident = await db.Incidents.FindAsync(incidentId);
            incident!.Status.Should().Be(IncidentStatuses.Executing);
        }
    }
}
