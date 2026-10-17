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
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Execution;
using Trinetra.Core.Models.Guardrails;

namespace Trinetra.Agents.AutoExecutor;

public class AutoExecutorAgent : AgentBase
{
    public const string AgentIdentifier = "auto_executor";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AutoExecutorPlugin _plugin;
    private readonly IGuardrailsEngine _guardrails;
    private readonly ICommandSandbox _sandbox;
    private readonly IIncidentCoordinatorStore _store;

    public AutoExecutorAgent(
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger<AutoExecutorAgent> logger,
        AutoExecutorPlugin plugin,
        IGuardrailsEngine guardrails,
        ICommandSandbox sandbox,
        IIncidentCoordinatorStore store,
        IAuditEventWriter? auditWriter = null)
        : base(AgentIdentifier, configuration, kernel, logger, auditWriter)
    {
        _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
        _guardrails = guardrails ?? throw new ArgumentNullException(nameof(guardrails));
        _sandbox = sandbox ?? throw new ArgumentNullException(nameof(sandbox));
        _store = store ?? throw new ArgumentNullException(nameof(store));

        RegisterPlugin(_plugin, "AutoExecutor");
    }

    public async Task<RemediationExecutionResult> ExecuteRemediationAsync(
        CommandExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        using var activity = StartAgentActivity("execute_remediation");
        if (activity != null)
        {
            activity.SetTag("trinetra.incident.id", request.IncidentId.ToString());
            activity.SetTag("trinetra.action.title", request.ActionTitle);
            activity.SetTag("trinetra.risk_level", request.RiskLevel);
        }

        var totalStopwatch = Stopwatch.StartNew();

        var executionResult = new RemediationExecutionResult
        {
            IncidentId = request.IncidentId,
            RemediationId = request.RemediationId,
            Action = request.ActionTitle,
            RollbackCommand = request.RollbackCommand
        };

        // 1. Audit Trail Ground Rule: Record start of execution attempt
        await RecordAuditEventAsync(
            request.IncidentId,
            "remediation_execution_started",
            $"Auto-Executor initiated execution for '{request.ActionTitle}' (Risk: {request.RiskLevel}, Approved: {request.IsApproved})",
            toolCall: "execute_remediation",
            toolParams: new { request.ActionTitle, request.Command, request.RiskLevel, request.IsApproved },
            requiresApproval: !request.IsApproved && request.RiskLevel != "low",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Determine all commands that need to run
        var commandsToRun = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Command))
        {
            commandsToRun.Add(request.Command);
        }
        if (request.Steps != null && request.Steps.Count > 0)
        {
            foreach (var step in request.Steps)
            {
                if (!string.IsNullOrWhiteSpace(step) && !commandsToRun.Contains(step))
                {
                    commandsToRun.Add(step);
                }
            }
        }

        if (commandsToRun.Count == 0)
        {
            totalStopwatch.Stop();
            executionResult.Status = ExecutionStatus.Failed;
            executionResult.ErrorMessage = "No executable command or steps provided.";

            await RecordAuditEventAsync(
                request.IncidentId,
                "remediation_execution_failed",
                "Execution aborted: No executable command or steps provided.",
                executionStatus: "failed",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return executionResult;
        }

        // 2. Non-Negotiable Ground Rule: Guardrail check on ALL proposed commands
        var guardrailContext = new GuardrailContext
        {
            IncidentId = request.IncidentId,
            AgentName = AgentIdentifier,
            RiskLevel = request.RiskLevel,
            IsApproved = request.IsApproved
        };

        foreach (var cmd in commandsToRun)
        {
            var guardrailCheck = await _guardrails.EvaluateCommandAsync(cmd, guardrailContext, cancellationToken).ConfigureAwait(false);

            if (guardrailCheck.IsBlocked)
            {
                totalStopwatch.Stop();
                executionResult.Status = ExecutionStatus.Blocked;
                executionResult.ErrorMessage = $"Blocked by guardrail: {guardrailCheck.Message}";

                Logger.LogWarning("Command blocked by guardrail for incident {IncidentId}: {Command}", request.IncidentId, cmd);

                // Audit trail: Record guardrail violation
                await RecordAuditEventAsync(
                    request.IncidentId,
                    "guardrail_violation",
                    $"Guardrail triggered: {guardrailCheck.Message}",
                    toolCall: "guardrail_check",
                    toolResult: JsonSerializer.Serialize(guardrailCheck.Violations, JsonOptions),
                    executionStatus: "blocked",
                    executionError: guardrailCheck.Message,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                // Escalate incident immediately upon guardrail blocker violation
                await _store.EscalateIncidentAsync(
                    request.IncidentId,
                    $"Destructive command blocked by safety guardrail: {guardrailCheck.Message}",
                    cancellationToken).ConfigureAwait(false);

                return executionResult;
            }

            if (guardrailCheck.Outcome == GuardrailOutcome.RequiresApproval && !request.IsApproved)
            {
                totalStopwatch.Stop();
                executionResult.Status = ExecutionStatus.Blocked;
                executionResult.ErrorMessage = "Command requires explicit human approval before execution.";

                await RecordAuditEventAsync(
                    request.IncidentId,
                    "approval_required_unmet",
                    $"Execution rejected: Command '{cmd}' requires human approval which has not been granted.",
                    toolCall: "guardrail_check",
                    executionStatus: "blocked",
                    requiresApproval: true,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                await _store.UpdateIncidentStatusAsync(
                    request.IncidentId,
                    IncidentStatuses.AwaitingApproval,
                    "Command requires human approval",
                    cancellationToken).ConfigureAwait(false);

                return executionResult;
            }
        }

        // 3. Pre-flight checks
        if (request.PreflightChecks != null && request.PreflightChecks.Count > 0)
        {
            foreach (var check in request.PreflightChecks)
            {
                var checkJson = _plugin.RunPreflightCheck(check);
                var checkResult = JsonSerializer.Deserialize<CheckResult>(checkJson, JsonOptions) ?? new CheckResult { Check = check, Status = "failed" };
                executionResult.PreflightChecks.Add(checkResult);

                if (checkResult.Status != "passed")
                {
                    totalStopwatch.Stop();
                    executionResult.Status = ExecutionStatus.Failed;
                    executionResult.ErrorMessage = $"Pre-flight check failed: '{check}'";

                    await RecordAuditEventAsync(
                        request.IncidentId,
                        "preflight_check_failed",
                        $"Pre-flight check failed: {checkResult.Message}",
                        toolCall: "run_preflight_checks",
                        toolResult: checkJson,
                        executionStatus: "failed",
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    await _store.EscalateIncidentAsync(
                        request.IncidentId,
                        $"Pre-flight check failed: {check}",
                        cancellationToken).ConfigureAwait(false);

                    return executionResult;
                }
            }
        }

        // 4. Transition incident state to Executing
        await _store.UpdateIncidentStatusAsync(
            request.IncidentId,
            IncidentStatuses.Executing,
            "Remediation commands executing in sandbox",
            cancellationToken).ConfigureAwait(false);

        // 5. Execute commands in sandbox step-by-step
        int stepIndex = 1;
        bool executionFailed = false;
        string? failureReason = null;

        foreach (var cmd in commandsToRun)
        {
            var stepSw = Stopwatch.StartNew();
            var stepJson = await _plugin.ExecuteRemediationCommandAsync(cmd, request.TimeoutSeconds, cancellationToken).ConfigureAwait(false);
            stepSw.Stop();

            var stepResult = JsonSerializer.Deserialize<ExecutionStepResult>(stepJson, JsonOptions) ?? new ExecutionStepResult
            {
                Step = stepIndex,
                Command = cmd,
                Status = ExecutionStatus.Failed
            };
            stepResult.Step = stepIndex++;
            executionResult.ExecutionSteps.Add(stepResult);

            // Audit Trail Ground Rule: Record step execution
            await RecordAuditEventAsync(
                request.IncidentId,
                "remediation_step_executed",
                $"Step {stepResult.Step} executed: {cmd} (Status: {stepResult.Status}, ExitCode: {stepResult.ExitCode}, Duration: {stepResult.DurationMs}ms)",
                toolCall: "execute_remediation_command",
                toolParams: new { cmd, request.TimeoutSeconds },
                toolResult: stepJson,
                executionStatus: stepResult.Status == ExecutionStatus.Success ? "success" : "failed",
                executionError: stepResult.ErrorMessage ?? (stepResult.ExitCode != 0 ? stepResult.StandardError : null),
                executionTimeMs: stepResult.DurationMs,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (stepResult.Status != ExecutionStatus.Success)
            {
                executionFailed = true;
                failureReason = stepResult.ErrorMessage ?? $"Step exited with code {stepResult.ExitCode}: {stepResult.StandardError}";
                break;
            }
        }

        // 6. If execution failed, trigger rollback
        if (executionFailed)
        {
            await HandleRollbackAsync(request, executionResult, failureReason ?? "Command execution failed", cancellationToken).ConfigureAwait(false);
            totalStopwatch.Stop();
            executionResult.TotalDurationSeconds = (int)totalStopwatch.Elapsed.TotalSeconds;
            return executionResult;
        }

        // 7. Post-flight checks
        if (request.PostflightChecks != null && request.PostflightChecks.Count > 0)
        {
            foreach (var check in request.PostflightChecks)
            {
                var checkJson = _plugin.RunPostflightCheck(check);
                var checkResult = JsonSerializer.Deserialize<CheckResult>(checkJson, JsonOptions) ?? new CheckResult { Check = check, Status = "failed" };
                executionResult.PostflightChecks.Add(checkResult);

                if (checkResult.Status != "passed")
                {
                    Logger.LogWarning("Post-flight check failed for incident {IncidentId}: {Check}", request.IncidentId, check);

                    await RecordAuditEventAsync(
                        request.IncidentId,
                        "postflight_check_failed",
                        $"Post-flight check failed: {checkResult.Message}",
                        toolCall: "run_postflight_checks",
                        toolResult: checkJson,
                        executionStatus: "failed",
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    await HandleRollbackAsync(request, executionResult, $"Post-flight check failed: {check}", cancellationToken).ConfigureAwait(false);
                    totalStopwatch.Stop();
                    executionResult.TotalDurationSeconds = (int)totalStopwatch.Elapsed.TotalSeconds;
                    return executionResult;
                }
            }
        }

        // 8. All steps and checks succeeded -> Resolve incident!
        totalStopwatch.Stop();
        executionResult.Status = ExecutionStatus.Success;
        executionResult.TotalDurationSeconds = (int)totalStopwatch.Elapsed.TotalSeconds;

        await _store.UpdateIncidentStatusAsync(
            request.IncidentId,
            IncidentStatuses.Resolved,
            "Remediation successfully executed and verified",
            cancellationToken).ConfigureAwait(false);

        await RecordAuditEventAsync(
            request.IncidentId,
            "remediation_execution_completed",
            $"Remediation successfully executed and validated in {executionResult.TotalDurationSeconds}s. Incident resolved.",
            toolCall: "execute_remediation",
            toolResult: JsonSerializer.Serialize(executionResult, JsonOptions),
            executionStatus: "success",
            executionTimeMs: (int)totalStopwatch.ElapsedMilliseconds,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("Incident {IncidentId} remediation execution completed successfully in {Duration}s",
            request.IncidentId, executionResult.TotalDurationSeconds);

        return executionResult;
    }

    private async Task HandleRollbackAsync(
        CommandExecutionRequest request,
        RemediationExecutionResult executionResult,
        string failureReason,
        CancellationToken cancellationToken)
    {
        executionResult.Status = ExecutionStatus.Failed;
        executionResult.ErrorMessage = failureReason;

        if (!string.IsNullOrWhiteSpace(request.RollbackCommand))
        {
            Logger.LogWarning("Initiating rollback for incident {IncidentId}: {RollbackCommand}", request.IncidentId, request.RollbackCommand);

            // Guardrail check on rollback command
            var guardContext = new GuardrailContext
            {
                IncidentId = request.IncidentId,
                AgentName = AgentIdentifier,
                RiskLevel = "medium",
                IsApproved = true // Rollbacks of authorized remediations are allowed
            };

            var rollbackGuard = await _guardrails.EvaluateCommandAsync(request.RollbackCommand, guardContext, cancellationToken).ConfigureAwait(false);
            if (rollbackGuard.IsBlocked)
            {
                Logger.LogError("Rollback command blocked by guardrail: {Message}", rollbackGuard.Message);
                executionResult.RollbackExecuted = true;
                executionResult.RollbackSuccess = false;

                await RecordAuditEventAsync(
                    request.IncidentId,
                    "rollback_guardrail_violation",
                    $"Rollback command blocked by guardrail: {rollbackGuard.Message}",
                    executionStatus: "blocked",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var rollbackJson = await _plugin.ExecuteRollbackAsync(request.RollbackCommand, cancellationToken: cancellationToken).ConfigureAwait(false);
                var rollbackStep = JsonSerializer.Deserialize<ExecutionStepResult>(rollbackJson, JsonOptions);

                executionResult.RollbackExecuted = true;
                executionResult.RollbackSuccess = rollbackStep?.Status == ExecutionStatus.Success;

                await RecordAuditEventAsync(
                    request.IncidentId,
                    "remediation_rollback_executed",
                    $"Rollback executed: {request.RollbackCommand} (Success: {executionResult.RollbackSuccess})",
                    toolCall: "execute_rollback",
                    toolParams: new { request.RollbackCommand },
                    toolResult: rollbackJson,
                    executionStatus: executionResult.RollbackSuccess ? "success" : "failed",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }

        // Always escalate the incident when remediation fails or rolls back
        await _store.EscalateIncidentAsync(
            request.IncidentId,
            $"Remediation failed: {failureReason}. Incident escalated for human SRE intervention.",
            cancellationToken).ConfigureAwait(false);
    }
}
