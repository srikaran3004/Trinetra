using System.Threading.Tasks;
using Trinetra.Agents.Guardrails;
using Trinetra.Core.Models.Guardrails;
using Xunit;

namespace Trinetra.UnitTests.Guardrails;

/// <summary>
/// Tests for GuardrailsEngine.
/// Ground rule: tests written BEFORE implementation.
/// All destructive / sensitive commands must be blocked (outcome = Blocked).
/// Medium/high-risk commands with no approval must return RequiresApproval.
/// Safe diagnostic commands must return Allowed.
/// </summary>
public sealed class GuardrailsEngineTests
{
    private readonly GuardrailsEngine _engine = new();

    // ── Blocked commands (Type 1: Blockers) ─────────────────────────────────

    [Theory]
    [InlineData("rm -rf /")]
    [InlineData("rm -rf /*")]
    [InlineData("rm -rf /etc")]
    [InlineData("sudo rm -rf /")]
    public async Task BlockedCommand_RecursiveDelete_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
        Assert.NotEmpty(result.Violations);
        Assert.Contains(result.Violations, v => v.IsBlocker);
    }

    [Theory]
    [InlineData("shutdown now")]
    [InlineData("shutdown -h now")]
    [InlineData("reboot")]
    [InlineData("halt")]
    public async Task BlockedCommand_SystemShutdown_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
    }

    [Theory]
    [InlineData("mkfs.ext4 /dev/sda")]
    [InlineData("mkfs /dev/sdb1")]
    [InlineData("dd if=/dev/zero of=/dev/sda")]
    [InlineData("dd if=/dev/urandom of=/dev/sda1")]
    public async Task BlockedCommand_DiskDestruction_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
    }

    [Theory]
    [InlineData("dropdb mydb")]
    [InlineData("DROP DATABASE production")]
    [InlineData("DROP DATABASE IF EXISTS myapp")]
    [InlineData("TRUNCATE TABLE users")]
    [InlineData("TRUNCATE users CASCADE")]
    [InlineData("DROP TABLE incidents")]
    public async Task BlockedCommand_DatabaseDestruction_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
    }

    [Theory]
    [InlineData("cat /etc/shadow")]
    [InlineData("cat /etc/passwd")]
    [InlineData("sudo cat /etc/shadow")]
    public async Task BlockedCommand_SensitiveFileAccess_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
    }

    [Theory]
    [InlineData("kill -9 1")]
    [InlineData("kill -9 123")]
    [InlineData("kill -KILL 456")]
    public async Task BlockedCommand_KillSignal9_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
    }

    [Theory]
    [InlineData("iptables -F")]
    [InlineData("iptables -D INPUT 1")]
    [InlineData("ip route del default")]
    [InlineData("ip route flush")]
    public async Task BlockedCommand_NetworkModification_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        Assert.True(result.IsBlocked);
    }

    // ── Case-insensitive blocking ────────────────────────────────────────────

    [Theory]
    [InlineData("DROP DATABASE Production")]
    [InlineData("Drop Database mydb")]
    [InlineData("TRUNCATE TABLE Users")]
    [InlineData("Truncate users")]
    public async Task BlockedCommand_CaseInsensitive_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
    }

    // ── Commands requiring approval (Type 2: Approvals) ──────────────────────

    [Theory]
    [InlineData("kubectl scale deployment myapp --replicas=2")]
    [InlineData("kubectl rollout undo deployment/myapp")]
    [InlineData("pg_ctl restart")]
    [InlineData("systemctl restart postgresql")]
    public async Task ApprovalCommand_WithoutApproval_RequiresApproval(string command)
    {
        var ctx = new GuardrailContext { RiskLevel = "medium", IsApproved = false };
        var result = await _engine.EvaluateCommandAsync(command, ctx);

        Assert.Equal(GuardrailOutcome.RequiresApproval, result.Outcome);
        Assert.False(result.IsBlocked);
        Assert.NotEmpty(result.Violations);
    }

    [Theory]
    [InlineData("kubectl scale deployment myapp --replicas=2")]
    [InlineData("kubectl rollout undo deployment/myapp")]
    public async Task ApprovalCommand_WithApproval_IsAllowed(string command)
    {
        var ctx = new GuardrailContext { RiskLevel = "medium", IsApproved = true };
        var result = await _engine.EvaluateCommandAsync(command, ctx);

        Assert.Equal(GuardrailOutcome.Allowed, result.Outcome);
        Assert.False(result.IsBlocked);
    }

    // ── Allowed (safe diagnostic) commands ──────────────────────────────────

    [Theory]
    [InlineData("kubectl get pods")]
    [InlineData("kubectl describe pod mypod-abc123")]
    [InlineData("kubectl logs mypod-abc123")]
    [InlineData("kubectl top nodes")]
    [InlineData("ps aux")]
    [InlineData("df -h")]
    [InlineData("free -m")]
    [InlineData("netstat -tlnp")]
    [InlineData("ping -c 4 google.com")]
    [InlineData("curl -s http://localhost:8080/health")]
    [InlineData("SELECT pg_size_pretty(pg_database_size('mydb'))")]
    [InlineData("SELECT count(*) FROM pg_stat_activity WHERE state = 'active'")]
    public async Task AllowedCommand_SafeDiagnostics_IsAllowed(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Allowed, result.Outcome);
        Assert.False(result.IsBlocked);
        Assert.Empty(result.Violations);
    }

    // ── Empty / null / whitespace commands ──────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EmptyOrNullCommand_IsBlocked(string? command)
    {
        var result = await _engine.EvaluateCommandAsync(command!);

        // Empty commands should never execute
        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
    }

    // ── Bypass attempts ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("echo hello && rm -rf /")]
    [InlineData("ls; shutdown now")]
    [InlineData("kubectl get pods | grep -v hello && DROP DATABASE mydb")]
    public async Task BypassAttempt_ChainedDestructiveCommand_IsBlocked(string command)
    {
        var result = await _engine.EvaluateCommandAsync(command);

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
    }

    // ── Rate limits (sudo without approval) ──────────────────────────────────

    [Theory]
    [InlineData("sudo service nginx restart")]
    [InlineData("sudo systemctl stop httpd")]
    public async Task SudoWithoutApproval_WithoutApprovalContext_RequiresApproval(string command)
    {
        var ctx = new GuardrailContext { IsApproved = false };
        var result = await _engine.EvaluateCommandAsync(command, ctx);

        // sudo without approval must require explicit approval (not quietly allowed)
        Assert.NotEqual(GuardrailOutcome.Allowed, result.Outcome);
    }

    // ── Pattern accessors ────────────────────────────────────────────────────

    [Fact]
    public void GetBlockedPatterns_ReturnsNonEmptyList()
    {
        var patterns = _engine.GetBlockedPatterns();

        Assert.NotNull(patterns);
        Assert.NotEmpty(patterns);
    }

    [Fact]
    public void GetApprovalPatterns_ReturnsNonEmptyList()
    {
        var patterns = _engine.GetApprovalPatterns();

        Assert.NotNull(patterns);
        Assert.NotEmpty(patterns);
    }

    // ── Violation metadata ────────────────────────────────────────────────────

    [Fact]
    public async Task BlockedCommand_ViolationMetadata_IsComplete()
    {
        var result = await _engine.EvaluateCommandAsync("rm -rf /");

        Assert.Equal(GuardrailOutcome.Blocked, result.Outcome);
        var violation = Assert.Single(result.Violations);
        Assert.False(string.IsNullOrWhiteSpace(violation.RuleName));
        Assert.False(string.IsNullOrWhiteSpace(violation.Description));
        Assert.False(string.IsNullOrWhiteSpace(violation.MatchedPattern));
        Assert.True(violation.IsBlocker);
    }
}
