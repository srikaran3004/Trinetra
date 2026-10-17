using System.Text.RegularExpressions;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Guardrails;

namespace Trinetra.Agents.Guardrails;

/// <summary>
/// Custom C# guardrails engine implementing <see cref="IGuardrailsEngine"/>.
///
/// Design principles:
///  1. Blockers fire first — if ANY blocked pattern matches, the result is always Blocked.
///  2. No configuration flag can disable a blocker; the lists are baked into the code.
///  3. Approval patterns only promote to RequiresApproval; a pre-existing Blocked outcome
///     is never downgraded.
///  4. An already-approved context (IsApproved = true) lifts RequiresApproval → Allowed,
///     but NEVER lifts Blocked → Allowed.
///  5. Matching is case-insensitive throughout.
///  6. Empty / null / whitespace commands are always blocked.
/// </summary>
public sealed class GuardrailsEngine : IGuardrailsEngine
{
    // ── Blocked patterns (Type 1 — never execute, not overridable) ──────────

    private static readonly IReadOnlyList<BlockedRule> s_blockedRules =
    [
        // Recursive destructive deletes
        new("block_recursive_delete",   @"rm\s+-[^\s]*r[^\s]*\s+/",
            "Recursive delete from root or root subdirectory is never permitted."),
        new("block_recursive_delete",   @"rm\s+-[^\s]*f[^\s]*\s+/\*",
            "Wildcard recursive delete from root is never permitted."),

        // System shutdown / restart
        new("block_system_shutdown",    @"\bshutdown\b",
            "System shutdown is blocked."),
        new("block_system_shutdown",    @"\breboot\b",
            "System reboot is blocked."),
        new("block_system_shutdown",    @"\bhalt\b",
            "System halt is blocked."),

        // Disk / filesystem destruction
        new("block_disk_destruction",   @"\bmkfs\b",
            "Filesystem formatting (mkfs) is never permitted."),
        new("block_disk_destruction",   @"\bdd\b.*\bif=/dev/zero\b",
            "Zeroing a device with dd is never permitted."),
        new("block_disk_destruction",   @"\bdd\b.*\bif=/dev/urandom\b",
            "Randomising a device with dd is never permitted."),

        // Database destruction (shell utilities)
        new("block_db_destruction",     @"\bdropdb\b",
            "dropdb utility is blocked — it permanently destroys a database."),

        // Database destruction (SQL DDL)
        new("block_sql_drop_database",  @"\bdrop\s+database\b",
            "DROP DATABASE SQL statement is blocked."),
        new("block_sql_drop_table",     @"\bdrop\s+table\b",
            "DROP TABLE SQL statement is blocked."),
        new("block_sql_truncate",       @"\btruncate\s+(table\s+)?\w+",
            "TRUNCATE TABLE SQL statement is blocked."),

        // Sensitive file access
        new("block_sensitive_file",     @"\bcat\b.*(/etc/shadow|/etc/passwd)",
            "Access to /etc/shadow or /etc/passwd is blocked."),

        // Kill signal 9 (SIGKILL — cannot be caught or ignored)
        new("block_kill_sigkill",       @"\bkill\s+(-9|-KILL)\b",
            "SIGKILL (kill -9) is blocked to prevent killing critical processes."),

        // Network topology modification
        new("block_network_modification", @"\biptables\b",
            "Direct iptables modification is blocked; use a change-management workflow."),
        new("block_network_modification", @"\bip\s+route\b.*\b(del|flush)\b",
            "IP route deletion/flush is blocked."),
    ];

    // ── Approval patterns (Type 2 — allowed only with explicit human approval) ──

    private static readonly IReadOnlyList<ApprovalRule> s_approvalRules =
    [
        new("require_approval_kubectl_scale",
            @"\bkubectl\s+scale\b",
            "Scaling Kubernetes deployments requires approval."),
        new("require_approval_kubectl_rollout_undo",
            @"\bkubectl\s+rollout\s+undo\b",
            "Rolling back a Kubernetes deployment requires approval."),
        new("require_approval_pg_ctl",
            @"\bpg_ctl\b",
            "PostgreSQL server control (pg_ctl) requires approval."),
        new("require_approval_service_restart",
            @"\bsystemctl\s+(restart|stop|disable)\b",
            "systemctl restart/stop/disable requires approval."),
        new("require_approval_sudo",
            @"^sudo\b",
            "sudo commands require explicit approval."),
        new("require_approval_sudo_inline",
            @"\bsudo\b",
            "Commands containing sudo require explicit approval."),
    ];

    // Compiled regex caches (case-insensitive)
    private static readonly IReadOnlyList<(Regex Rx, BlockedRule Rule)> s_compiledBlocked =
        s_blockedRules
            .Select(r => (new Regex(r.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), r))
            .ToList();

    private static readonly IReadOnlyList<(Regex Rx, ApprovalRule Rule)> s_compiledApproval =
        s_approvalRules
            .Select(r => (new Regex(r.Pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled), r))
            .ToList();

    // ── IGuardrailsEngine ────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<GuardrailResult> EvaluateCommandAsync(
        string command,
        GuardrailContext? context = null,
        CancellationToken cancellationToken = default)
    {
        // Rule 0: Empty / null / whitespace — always blocked
        if (string.IsNullOrWhiteSpace(command))
        {
            return Task.FromResult(GuardrailResult.Block(
                [new GuardrailViolation
                {
                    RuleName = "block_empty_command",
                    Description = "Empty or whitespace commands are never permitted.",
                    MatchedPattern = "(empty)",
                    IsBlocker = true
                }],
                "Command is empty or blank and cannot be executed."));
        }

        // Rule 1: Check blockers (highest priority — not overridable)
        var blockerViolation = FindFirstBlockerViolation(command);
        if (blockerViolation is not null)
        {
            return Task.FromResult(GuardrailResult.Block(
                [blockerViolation],
                $"Command is blocked by guardrail '{blockerViolation.RuleName}': {blockerViolation.Description}"));
        }

        // Rule 2: Check approval patterns
        var approvalViolations = FindApprovalViolations(command);
        if (approvalViolations.Count > 0)
        {
            // If the caller has already obtained human approval, allow execution
            if (context?.IsApproved == true)
            {
                return Task.FromResult(
                    GuardrailResult.Allow("Command requires approval — approval already obtained; proceeding."));
            }

            return Task.FromResult(GuardrailResult.Approve(
                approvalViolations,
                $"Command requires human approval before execution. " +
                $"Triggered rules: {string.Join(", ", approvalViolations.Select(v => v.RuleName))}."));
        }

        // Rule 3: All clear — allowed
        return Task.FromResult(GuardrailResult.Allow());
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetBlockedPatterns() =>
        s_blockedRules.Select(r => r.Pattern).ToList();

    /// <inheritdoc />
    public IReadOnlyList<string> GetApprovalPatterns() =>
        s_approvalRules.Select(r => r.Pattern).ToList();

    // ── Private helpers ──────────────────────────────────────────────────────

    private static GuardrailViolation? FindFirstBlockerViolation(string command)
    {
        foreach (var (rx, rule) in s_compiledBlocked)
        {
            var match = rx.Match(command);
            if (match.Success)
            {
                return new GuardrailViolation
                {
                    RuleName = rule.RuleName,
                    Description = rule.Description,
                    MatchedPattern = match.Value,
                    IsBlocker = true
                };
            }
        }
        return null;
    }

    private static IReadOnlyList<GuardrailViolation> FindApprovalViolations(string command)
    {
        var violations = new List<GuardrailViolation>();
        var seenRules = new HashSet<string>();

        foreach (var (rx, rule) in s_compiledApproval)
        {
            var match = rx.Match(command);
            if (match.Success && seenRules.Add(rule.RuleName))
            {
                violations.Add(new GuardrailViolation
                {
                    RuleName = rule.RuleName,
                    Description = rule.Description,
                    MatchedPattern = match.Value,
                    IsBlocker = false
                });
            }
        }

        return violations;
    }

    // ── Inner rule record types ──────────────────────────────────────────────

    private sealed record BlockedRule(string RuleName, string Pattern, string Description);
    private sealed record ApprovalRule(string RuleName, string Pattern, string Description);
}
