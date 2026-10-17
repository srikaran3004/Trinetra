using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbApprovalPolicyRepository : IApprovalPolicyRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbApprovalPolicyRepository> _logger;

    public DbApprovalPolicyRepository(
        ApplicationDbContext dbContext,
        ILogger<DbApprovalPolicyRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<ApprovalPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var policies = await _dbContext.ApprovalPolicies
                .Where(p => p.IsActive)
                .OrderBy(p => p.Priority)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (policies.Count > 0)
            {
                return policies;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query active approval policies from database. Falling back to default policies.");
        }

        return GetDefaultPolicies();
    }

    public async Task<ApprovalPolicy?> FindMatchingPolicyAsync(
        string? command,
        string? service,
        string? riskLevel,
        string? severity,
        CancellationToken cancellationToken = default)
    {
        var policies = await GetActivePoliciesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var policy in policies.OrderBy(p => p.Priority))
        {
            // 1. Check command match
            bool commandMatches = policy.AppliesToCommands == null ||
                                  policy.AppliesToCommands.Count == 0 ||
                                  (!string.IsNullOrWhiteSpace(command) && policy.AppliesToCommands.Any(c =>
                                      string.Equals(c, command, StringComparison.OrdinalIgnoreCase) ||
                                      command.Contains(c, StringComparison.OrdinalIgnoreCase)));

            // 2. Check service match
            bool serviceMatches = policy.AppliesToServices == null ||
                                  policy.AppliesToServices.Count == 0 ||
                                  (!string.IsNullOrWhiteSpace(service) && policy.AppliesToServices.Any(s =>
                                      string.Equals(s, service, StringComparison.OrdinalIgnoreCase)));

            // 3. Check severity match
            bool severityMatches = policy.AppliesToIncidentSeverity == null ||
                                   policy.AppliesToIncidentSeverity.Count == 0 ||
                                   (!string.IsNullOrWhiteSpace(severity) && policy.AppliesToIncidentSeverity.Any(sev =>
                                       string.Equals(sev, severity, StringComparison.OrdinalIgnoreCase)));

            // 4. Check min risk level
            bool riskMatches = string.IsNullOrWhiteSpace(policy.MinRiskLevel) ||
                               IsRiskAtLeast(riskLevel, policy.MinRiskLevel);

            if (commandMatches && serviceMatches && severityMatches && riskMatches)
            {
                return policy;
            }
        }

        return null;
    }

    private static bool IsRiskAtLeast(string? currentRisk, string minRisk)
    {
        var order = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["low"] = 1,
            ["medium"] = 2,
            ["high"] = 3
        };

        int currentRank = !string.IsNullOrWhiteSpace(currentRisk) && order.TryGetValue(currentRisk, out var c) ? c : 1;
        int minRank = order.TryGetValue(minRisk, out var m) ? m : 1;

        return currentRank >= minRank;
    }

    private static List<ApprovalPolicy> GetDefaultPolicies()
    {
        return new List<ApprovalPolicy>
        {
            new()
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
                RuleName = "high_risk_remediation_approval",
                Description = "Require lead or manager approval for high risk actions",
                Priority = 10,
                AppliesToCommands = new List<string> { "database_failover", "scale_down_pods", "restart_critical_service", "kill_long_queries" },
                MinRiskLevel = "high",
                AppliesToIncidentSeverity = new List<string> { "critical", "high" },
                RequiresApproval = true,
                ApprovalLevel = "lead",
                EscalationTimeoutMinutes = 15,
                IsActive = true
            },
            new()
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000002"),
                RuleName = "medium_risk_deployment_rollback",
                Description = "Require SRE or lead approval for production deployment rollbacks",
                Priority = 20,
                AppliesToCommands = new List<string> { "deployment_rollback", "kill_idle_queries" },
                MinRiskLevel = "medium",
                AppliesToIncidentSeverity = new List<string> { "critical", "high", "medium" },
                RequiresApproval = true,
                ApprovalLevel = "sre",
                EscalationTimeoutMinutes = 15,
                IsActive = true
            }
        };
    }
}
