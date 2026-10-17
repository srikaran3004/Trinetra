using Trinetra.Core.Models.Guardrails;

namespace Trinetra.Core.Interfaces;

/// <summary>
/// Non-bypassable guardrail engine that must be called before any command executes.
/// No code path may execute a shell or SQL command without first obtaining an
/// <see cref="GuardrailResult"/> with <see cref="GuardrailOutcome.Allowed"/> from this service.
/// </summary>
public interface IGuardrailsEngine
{
    /// <summary>
    /// Evaluates a proposed command string against all safety rules.
    /// </summary>
    /// <param name="command">The raw command (shell or SQL) the agent wants to execute.</param>
    /// <param name="context">Optional context about the incident, agent, and approval state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="GuardrailResult"/> that is <see cref="GuardrailOutcome.Allowed"/>,
    /// <see cref="GuardrailOutcome.Blocked"/>, or <see cref="GuardrailOutcome.RequiresApproval"/>.
    /// </returns>
    Task<GuardrailResult> EvaluateCommandAsync(
        string command,
        GuardrailContext? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full set of blocked-command patterns currently loaded.
    /// Used for diagnostics and audit reporting only; never for bypass logic.
    /// </summary>
    IReadOnlyList<string> GetBlockedPatterns();

    /// <summary>
    /// Returns the full set of approval-required patterns currently loaded.
    /// </summary>
    IReadOnlyList<string> GetApprovalPatterns();
}
