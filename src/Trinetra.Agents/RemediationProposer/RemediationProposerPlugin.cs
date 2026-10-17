using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Analysis;
using Trinetra.Core.Models.Remediation;

namespace Trinetra.Agents.RemediationProposer;

public class RemediationProposerPlugin
{
    private readonly IApprovalPolicyRepository _policyRepo;
    private readonly ILogger<RemediationProposerPlugin> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RemediationProposerPlugin(
        IApprovalPolicyRepository policyRepo,
        ILogger<RemediationProposerPlugin> logger)
    {
        _policyRepo = policyRepo ?? throw new ArgumentNullException(nameof(policyRepo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("generate_remediation_options")]
    [Description("Generates 2 to 3 structured remediation options based on root cause findings and retrieved runbooks")]
    public string GenerateRemediationOptions(
        [Description("Root cause category (DEPLOYMENT, RESOURCE, DATABASE, etc.)")] string category,
        [Description("Root cause summary description")] string rootCause,
        [Description("Target service name")] string? serviceName = null,
        [Description("Runbook JSON string with retrieved runbooks and steps")] string? runbooksJson = null)
    {
        _logger.LogInformation("Tool generate_remediation_options invoked for category '{Category}'", category);

        var options = new List<RemediationOption>();
        var svc = string.IsNullOrWhiteSpace(serviceName) ? "service" : serviceName;

        switch (category?.ToUpperInvariant())
        {
            case RootCauseCategories.Deployment:
                options.Add(new RemediationOption
                {
                    OptionId = 1,
                    Title = $"Rollback Deployment for {svc}",
                    Description = $"Roll back {svc} to previous stable release to revert faulty code changes",
                    ActionCommand = "deployment_rollback",
                    ImplementationSteps = new List<string>
                    {
                        $"Initiate deployment rollback for {svc} via release pipeline",
                        $"Monitor rolling pod restart and verify health endpoints",
                        "Verify request latency and error rate return to normal"
                    },
                    RiskLevel = RemediationRiskLevels.Medium,
                    RiskFactors = new List<string> { "Temporary traffic reroute during pod rolling restart" },
                    EstimatedTimeMinutes = 5,
                    RequiresApproval = true,
                    ApprovalLevel = "sre",
                    SuccessProbability = 0.95,
                    Recommended = true,
                    PreflightChecks = new List<string>
                    {
                        "Verify previous release artifact exists in registry",
                        "Confirm deployment pipeline is accessible"
                    },
                    PostflightChecks = new List<string>
                    {
                        "Verify pods healthy and reporting 200 OK",
                        "Confirm error rates drop below 1%"
                    }
                });

                options.Add(new RemediationOption
                {
                    OptionId = 2,
                    Title = $"Restart {svc} Pods and Flush In-Memory Cache",
                    Description = "Perform graceful rolling restart of pods and flush connection caches",
                    ActionCommand = "restart_service_pods",
                    ImplementationSteps = new List<string>
                    {
                        $"Trigger rolling restart of deployment/{svc}",
                        "Flush local Redis cache entries"
                    },
                    RiskLevel = RemediationRiskLevels.Low,
                    RiskFactors = new List<string>(),
                    EstimatedTimeMinutes = 3,
                    RequiresApproval = false,
                    ApprovalLevel = null,
                    SuccessProbability = 0.60,
                    Recommended = false,
                    PreflightChecks = new List<string> { "Ensure minimum available pod replica count >= 2" },
                    PostflightChecks = new List<string> { "Check application startup logs for errors" }
                });
                break;

            case RootCauseCategories.Database:
                options.Add(new RemediationOption
                {
                    OptionId = 1,
                    Title = "Terminate Long-Running Idle & Blocking Database Queries",
                    Description = "Identify and terminate queries blocking the connection pool to free connection slots",
                    ActionCommand = "kill_idle_queries",
                    ImplementationSteps = new List<string>
                    {
                        "Query: SELECT pid, state, query_start FROM pg_stat_activity WHERE state != 'idle' AND query_start < now() - interval '3 minutes'",
                        "Execute pg_terminate_backend(pid) on identified blocked queries",
                        "Verify available connection count recovers"
                    },
                    RiskLevel = RemediationRiskLevels.Medium,
                    RiskFactors = new List<string>
                    {
                        "May abort active in-flight transactions",
                        "Clients may receive connection reset errors"
                    },
                    EstimatedTimeMinutes = 3,
                    RequiresApproval = true,
                    ApprovalLevel = "lead",
                    SuccessProbability = 0.85,
                    Recommended = true,
                    PreflightChecks = new List<string>
                    {
                        "Verify database connection pool status",
                        "Confirm replication lag < 1 second"
                    },
                    PostflightChecks = new List<string>
                    {
                        "Confirm database connection pool utilization < 75%",
                        "Verify API p95 response time stabilizes"
                    }
                });

                options.Add(new RemediationOption
                {
                    OptionId = 2,
                    Title = "Increase Connection Pool Capacity & Replicas",
                    Description = "Dynamically scale connection pool size and route read traffic to read replica",
                    ActionCommand = "scale_db_pool",
                    ImplementationSteps = new List<string>
                    {
                        "Update max_connections parameter dynamically or scale pool configuration",
                        "Reload database configuration without restart"
                    },
                    RiskLevel = RemediationRiskLevels.Low,
                    RiskFactors = new List<string>(),
                    EstimatedTimeMinutes = 4,
                    RequiresApproval = false,
                    ApprovalLevel = null,
                    SuccessProbability = 0.70,
                    Recommended = false,
                    PreflightChecks = new List<string> { "Verify database server memory has headroom (> 20%)" },
                    PostflightChecks = new List<string> { "Verify new connections successfully established" }
                });

                options.Add(new RemediationOption
                {
                    OptionId = 3,
                    Title = "Database Primary Failover to Standby Replica",
                    Description = "Promote high-availability standby replica to primary if primary hardware is unresponsive",
                    ActionCommand = "database_failover",
                    ImplementationSteps = new List<string>
                    {
                        "Initiate automated failover via cluster orchestrator",
                        "Update DNS / connection endpoints to standby node",
                        "Validate replication consistency"
                    },
                    RiskLevel = RemediationRiskLevels.High,
                    RiskFactors = new List<string>
                    {
                        "Brief 15-30 second write freeze during promotion",
                        "Potential minimal replication data lag"
                    },
                    EstimatedTimeMinutes = 10,
                    RequiresApproval = true,
                    ApprovalLevel = "director",
                    SuccessProbability = 0.90,
                    Recommended = false,
                    PreflightChecks = new List<string> { "Confirm standby replica is synchronous and healthy" },
                    PostflightChecks = new List<string> { "Verify write queries succeed on promoted primary" }
                });
                break;

            case RootCauseCategories.Resource:
                options.Add(new RemediationOption
                {
                    OptionId = 1,
                    Title = $"Horizontal Pod Autoscaler (HPA) Scale Up for {svc}",
                    Description = $"Scale out {svc} pod replica count to distribute high CPU/memory load",
                    ActionCommand = "scale_up_pods",
                    ImplementationSteps = new List<string>
                    {
                        $"Execute kubectl scale deployment/{svc} --replicas=8",
                        "Wait for new pods to reach Ready state",
                        "Verify traffic load balances across expanded cluster"
                    },
                    RiskLevel = RemediationRiskLevels.Low,
                    RiskFactors = new List<string>(),
                    EstimatedTimeMinutes = 2,
                    RequiresApproval = false,
                    ApprovalLevel = null,
                    SuccessProbability = 0.90,
                    Recommended = true,
                    PreflightChecks = new List<string> { "Verify Kubernetes cluster node capacity has available CPU/RAM" },
                    PostflightChecks = new List<string> { "Confirm average CPU drops below 60%" }
                });

                options.Add(new RemediationOption
                {
                    OptionId = 2,
                    Title = $"Clear Redis Application Caches for {svc}",
                    Description = "Evict expired keys and clear cache buildup causing memory pressure",
                    ActionCommand = "clear_cache",
                    ImplementationSteps = new List<string>
                    {
                        $"Flush volatile cache keys for {svc}",
                        "Trigger GC collection cycle"
                    },
                    RiskLevel = RemediationRiskLevels.Low,
                    RiskFactors = new List<string>(),
                    EstimatedTimeMinutes = 1,
                    RequiresApproval = false,
                    ApprovalLevel = null,
                    SuccessProbability = 0.75,
                    Recommended = false,
                    PreflightChecks = new List<string> { "Ensure cache backend is responding" },
                    PostflightChecks = new List<string> { "Confirm memory utilization returns to baseline" }
                });
                break;

            default:
                options.Add(new RemediationOption
                {
                    OptionId = 1,
                    Title = $"Graceful Rolling Restart of {svc}",
                    Description = $"Restart {svc} instances to resolve transient issues or resource leaks",
                    ActionCommand = "restart_service_pods",
                    ImplementationSteps = new List<string>
                    {
                        $"Trigger rolling restart for {svc}",
                        "Monitor service logs and health check responses"
                    },
                    RiskLevel = RemediationRiskLevels.Low,
                    RiskFactors = new List<string>(),
                    EstimatedTimeMinutes = 3,
                    RequiresApproval = false,
                    ApprovalLevel = null,
                    SuccessProbability = 0.80,
                    Recommended = true,
                    PreflightChecks = new List<string> { "Ensure cluster is not in critical lockdown" },
                    PostflightChecks = new List<string> { "Confirm 200 OK health check" }
                });

                options.Add(new RemediationOption
                {
                    OptionId = 2,
                    Title = "Enable Circuit Breaker / Degraded Mode",
                    Description = "Temporarily return cached or degraded responses to prevent cascading outage",
                    ActionCommand = "enable_circuit_breaker",
                    ImplementationSteps = new List<string>
                    {
                        "Toggle circuit breaker flag via configuration API",
                        "Reroute traffic to static fallback page/cache"
                    },
                    RiskLevel = RemediationRiskLevels.Medium,
                    RiskFactors = new List<string> { "Users will experience partial feature degradation" },
                    EstimatedTimeMinutes = 2,
                    RequiresApproval = true,
                    ApprovalLevel = "lead",
                    SuccessProbability = 0.85,
                    Recommended = false,
                    PreflightChecks = new List<string> { "Verify fallback route is operational" },
                    PostflightChecks = new List<string> { "Verify core upstream services recover from pressure" }
                });
                break;
        }

        return JsonSerializer.Serialize(options, JsonOptions);
    }

    [KernelFunction("assess_remediation_risk")]
    [Description("Assesses the operational risk level ('low', 'medium', 'high') of a remediation command or action")]
    public string AssessRemediationRisk(
        [Description("Action or command name")] string command,
        [Description("Implementation steps or description")] string? description = null)
    {
        _logger.LogInformation("Tool assess_remediation_risk invoked for command '{Command}'", command);

        var text = $"{command} {description}".ToLowerInvariant();

        string riskLevel = RemediationRiskLevels.Low;
        var riskFactors = new List<string>();

        if (text.Contains("failover") || text.Contains("scale_down") || text.Contains("drain_node") ||
            text.Contains("truncate") || text.Contains("delete_database") || text.Contains("drop_table"))
        {
            riskLevel = RemediationRiskLevels.High;
            riskFactors.Add("Command involves infrastructure failover or irreversible data/capacity reduction");
            riskFactors.Add("Requires management or lead approval before execution");
        }
        else if (text.Contains("rollback") || text.Contains("kill") || text.Contains("circuit_breaker") ||
                 text.Contains("terminate") || text.Contains("patch_config"))
        {
            riskLevel = RemediationRiskLevels.Medium;
            riskFactors.Add("Action may terminate active user transactions or revert application versions");
            riskFactors.Add("Requires SRE or team lead approval");
        }
        else
        {
            riskLevel = RemediationRiskLevels.Low;
            riskFactors.Add("Safe autonomous execution with zero data risk");
        }

        return JsonSerializer.Serialize(new
        {
            command = command,
            risk_level = riskLevel,
            risk_factors = riskFactors
        }, JsonOptions);
    }

    [KernelFunction("evaluate_approval_policy")]
    [Description("Evaluates database approval policies for an action command to determine approval requirements")]
    public async Task<string> EvaluateApprovalPolicyAsync(
        [Description("Command name")] string command,
        [Description("Service name")] string? service = null,
        [Description("Assessed risk level ('low', 'medium', 'high')")] string? riskLevel = null,
        [Description("Incident severity ('critical', 'high', 'medium', 'low')")] string? severity = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool evaluate_approval_policy invoked for command '{Command}', risk '{Risk}'", command, riskLevel);

        var policy = await _policyRepo.FindMatchingPolicyAsync(command, service, riskLevel, severity, cancellationToken)
            .ConfigureAwait(false);

        if (policy != null && policy.RequiresApproval)
        {
            return JsonSerializer.Serialize(new
            {
                requires_approval = true,
                approval_level = policy.ApprovalLevel ?? "lead",
                policy_rule_name = policy.RuleName,
                escalation_timeout_minutes = policy.EscalationTimeoutMinutes
            }, JsonOptions);
        }

        // Default safety fallback: any medium or high risk requires approval if no policy explicitly allows it
        bool defaultApproval = string.Equals(riskLevel, RemediationRiskLevels.High, StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(riskLevel, RemediationRiskLevels.Medium, StringComparison.OrdinalIgnoreCase);

        return JsonSerializer.Serialize(new
        {
            requires_approval = defaultApproval,
            approval_level = defaultApproval ? (string.Equals(riskLevel, RemediationRiskLevels.High, StringComparison.OrdinalIgnoreCase) ? "director" : "lead") : null,
            policy_rule_name = "default_safety_policy",
            escalation_timeout_minutes = 15
        }, JsonOptions);
    }
}
