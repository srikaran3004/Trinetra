using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Coordinator;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Remediation;

namespace Trinetra.Agents.RemediationProposer;

public class RemediationProposerAgent : AgentBase
{
    public const string AgentIdentifier = "remediation_proposer";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly RemediationProposerPlugin _plugin;
    private readonly IIncidentCoordinatorStore _store;

    public RemediationProposerAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<RemediationProposerAgent> logger,
        RemediationProposerPlugin plugin,
        IIncidentCoordinatorStore store,
        IAuditEventWriter? auditWriter = null)
        : base(AgentIdentifier, configuration, kernel, logger, auditWriter)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _store = store ?? throw new ArgumentNullException(nameof(store));

        // Register tools into Semantic Kernel
        RegisterPlugin(_plugin, "RemediationProposer");
    }

    public async Task<RemediationProposalReport> ProposeRemediationAsync(
        Guid incidentId,
        string rootCause,
        string? rootCauseCategory = null,
        string? severity = null,
        string? serviceName = null,
        string? runbooksJson = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = StartAgentActivity("propose_remediation");
        if (activity != null)
        {
            activity.SetTag("trinetra.incident.id", incidentId.ToString());
            activity.SetTag("trinetra.root_cause_category", rootCauseCategory ?? "unknown");
        }

        var sw = Stopwatch.StartNew();

        // 1. Audit Trail Ground Rule: Record start of proposal
        await RecordAuditEventAsync(
            incidentId,
            "remediation_proposal_started",
            $"Remediation Proposer initiated remediation planning for incident {incidentId}",
            toolCall: "generate_remediation_options",
            toolParams: new { rootCause, rootCauseCategory, serviceName },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        try
        {
            // 2. Generate initial options from root cause and runbooks
            var optionsJson = _plugin.GenerateRemediationOptions(
                rootCauseCategory ?? "UNKNOWN",
                rootCause,
                serviceName,
                runbooksJson);

            var options = JsonSerializer.Deserialize<List<RemediationOption>>(optionsJson, JsonOptions) ?? new();

            // 3. For each option, assess risk and evaluate approval policy
            foreach (var opt in options)
            {
                // Risk assessment
                var riskJson = _plugin.AssessRemediationRisk(opt.ActionCommand ?? opt.Title, opt.Description);
                using (var doc = JsonDocument.Parse(riskJson))
                {
                    if (doc.RootElement.TryGetProperty("risk_level", out var rl))
                    {
                        opt.RiskLevel = rl.GetString() ?? opt.RiskLevel;
                    }
                }

                // Policy check
                var policyJson = await _plugin.EvaluateApprovalPolicyAsync(
                    opt.ActionCommand ?? opt.Title,
                    serviceName,
                    opt.RiskLevel,
                    severity,
                    cancellationToken).ConfigureAwait(false);

                using (var doc = JsonDocument.Parse(policyJson))
                {
                    if (doc.RootElement.TryGetProperty("requires_approval", out var req))
                    {
                        opt.RequiresApproval = req.GetBoolean();
                    }
                    if (doc.RootElement.TryGetProperty("approval_level", out var al))
                    {
                        opt.ApprovalLevel = al.GetString();
                    }
                }
            }

            // 4. Determine recommended option
            var recommended = options.FirstOrDefault(o => o.Recommended) ??
                              options.OrderByDescending(o => o.SuccessProbability).FirstOrDefault();

            if (recommended != null)
            {
                recommended.Recommended = true;
            }

            var report = new RemediationProposalReport
            {
                IncidentId = incidentId,
                RootCauseSummary = rootCause,
                RootCauseCategory = rootCauseCategory ?? "UNKNOWN",
                RemediationOptions = options,
                RecommendedOption = recommended,
                GeneratedAt = DateTime.UtcNow
            };

            // 5. Update database findings
            string? proposedJson = recommended != null ? JsonSerializer.Serialize(recommended, JsonOptions) : null;
            string? riskLevel = recommended?.RiskLevel;

            await _store.UpdateIncidentFindingsAsync(
                incidentId,
                rootCause: null,
                rootCauseCategory: null,
                confidence: null,
                proposedRemediation: proposedJson,
                riskLevel: riskLevel,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            sw.Stop();

            // 6. Audit Trail Ground Rule: Record completion
            await RecordAuditEventAsync(
                incidentId,
                "remediation_proposal_completed",
                $"Remediation proposal generated: {options.Count} options. Recommended: '{recommended?.Title}' (Risk: {recommended?.RiskLevel}, RequiresApproval: {recommended?.RequiresApproval})",
                toolCall: "generate_remediation_options",
                toolResult: JsonSerializer.Serialize(report, JsonOptions),
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                requiresApproval: recommended?.RequiresApproval ?? false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            Logger.LogInformation(
                "Remediation Proposal complete for {IncidentId}: {Count} options generated, Recommended='{Recommended}', Risk={Risk}, Duration={Elapsed}ms",
                incidentId, options.Count, recommended?.Title, riskLevel, sw.ElapsedMilliseconds);

            return report;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Logger.LogError(ex, "Failed to generate remediation proposal for incident {IncidentId}", incidentId);

            await RecordAuditEventAsync(
                incidentId,
                "remediation_proposal_failed",
                $"Remediation Proposer encountered an error: {ex.Message}",
                executionStatus: "failed",
                executionError: ex.Message,
                executionTimeMs: (int)sw.ElapsedMilliseconds,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            throw;
        }
    }
}
