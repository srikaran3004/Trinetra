using System.Collections.Generic;

namespace Trinetra.Core.Models.Guardrails;

/// <summary>
/// Outcome of a guardrail evaluation for a proposed command or action.
/// </summary>
public enum GuardrailOutcome
{
    /// <summary>The command is safe and may execute immediately.</summary>
    Allowed,

    /// <summary>The command is categorically blocked; execution is never permitted.</summary>
    Blocked,

    /// <summary>The command is allowed only after a human approves it.</summary>
    RequiresApproval
}

/// <summary>
/// Describes a single guardrail rule that was triggered during evaluation.
/// </summary>
public sealed class GuardrailViolation
{
    /// <summary>Short machine-readable name for the rule, e.g. "block_destructive_commands".</summary>
    public string RuleName { get; init; } = string.Empty;

    /// <summary>Human-readable description of why the rule fired.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>The exact pattern or token that matched.</summary>
    public string MatchedPattern { get; init; } = string.Empty;

    /// <summary>Whether this violation blocks execution outright (vs. requires approval).</summary>
    public bool IsBlocker { get; init; }
}

/// <summary>
/// The result returned by <c>IGuardrailsEngine.EvaluateCommandAsync</c>.
/// </summary>
public sealed class GuardrailResult
{
    /// <summary>Whether the command is allowed, blocked, or requires approval.</summary>
    public GuardrailOutcome Outcome { get; init; }

    /// <summary>List of violations that caused a non-Allowed outcome. Empty when Allowed.</summary>
    public IReadOnlyList<GuardrailViolation> Violations { get; init; } = [];

    /// <summary>Plain-English summary suitable for logging and UI display.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// True when the command must never be executed regardless of any override attempt.
    /// This property is deliberately kept separate from <see cref="Outcome"/> so callers
    /// cannot accidentally ignore blocked state by casting to bool.
    /// </summary>
    public bool IsBlocked => Outcome == GuardrailOutcome.Blocked;

    // ── Factory helpers ──────────────────────────────────────────────────────

    public static GuardrailResult Allow(string message = "Command is safe to execute.") =>
        new() { Outcome = GuardrailOutcome.Allowed, Message = message };

    public static GuardrailResult Block(IReadOnlyList<GuardrailViolation> violations, string message) =>
        new() { Outcome = GuardrailOutcome.Blocked, Violations = violations, Message = message };

    public static GuardrailResult Approve(IReadOnlyList<GuardrailViolation> violations, string message) =>
        new() { Outcome = GuardrailOutcome.RequiresApproval, Violations = violations, Message = message };
}

/// <summary>
/// Contextual information provided alongside the raw command string for richer evaluation.
/// </summary>
public sealed class GuardrailContext
{
    /// <summary>ID of the incident that triggered this command attempt.</summary>
    public Guid? IncidentId { get; init; }

    /// <summary>Name of the agent requesting execution.</summary>
    public string AgentName { get; init; } = string.Empty;

    /// <summary>Risk level declared by the proposer (low / medium / high).</summary>
    public string RiskLevel { get; init; } = "low";

    /// <summary>Whether a human has already approved this specific action.</summary>
    public bool IsApproved { get; init; }
}
