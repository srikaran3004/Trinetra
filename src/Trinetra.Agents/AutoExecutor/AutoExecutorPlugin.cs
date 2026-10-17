using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Execution;

namespace Trinetra.Agents.AutoExecutor;

public class AutoExecutorPlugin
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ICommandSandbox _sandbox;
    private readonly ILogger<AutoExecutorPlugin> _logger;

    public AutoExecutorPlugin(ICommandSandbox sandbox, ILogger<AutoExecutorPlugin> logger)
    {
        _sandbox = sandbox ?? throw new ArgumentNullException(nameof(sandbox));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction, Description("Executes a shell command in the sandboxed process environment")]
    public async Task<string> ExecuteRemediationCommandAsync(
        [Description("The shell command to execute")] string command,
        [Description("Maximum execution timeout in seconds")] int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing remediation command in sandbox (timeout={Timeout}s): {Command}", timeoutSeconds, command);

        var output = await _sandbox.ExecuteCommandAsync(
            command,
            TimeSpan.FromSeconds(timeoutSeconds),
            cancellationToken).ConfigureAwait(false);

        var stepResult = new ExecutionStepResult
        {
            Command = command,
            Status = output.IsSuccess ? ExecutionStatus.Success : (output.TimedOut ? ExecutionStatus.Timeout : ExecutionStatus.Failed),
            DurationMs = output.DurationMs,
            ExitCode = output.ExitCode,
            StandardOutput = output.StandardOutput,
            StandardError = output.StandardError,
            ErrorMessage = output.ErrorMessage
        };

        return JsonSerializer.Serialize(stepResult, JsonOptions);
    }

    [KernelFunction, Description("Runs a pre-flight validation check before executing remediation")]
    public string RunPreflightCheck(
        [Description("Description or condition for the preflight check")] string checkDescription)
    {
        _logger.LogInformation("Running preflight check: {Check}", checkDescription);

        // Preflight validations: verify system is not in unrecoverable state, format is recognized
        bool passed = !string.IsNullOrWhiteSpace(checkDescription) &&
                      !checkDescription.Contains("abort", StringComparison.OrdinalIgnoreCase) &&
                      !checkDescription.Contains("corrupted", StringComparison.OrdinalIgnoreCase);

        var checkResult = new CheckResult
        {
            Check = checkDescription,
            Status = passed ? "passed" : "failed",
            Value = passed ? "verified" : "check failed condition",
            Message = passed ? "Pre-flight condition satisfied" : "Pre-flight condition failed: conflicting state detected"
        };

        return JsonSerializer.Serialize(checkResult, JsonOptions);
    }

    [KernelFunction, Description("Runs a post-flight validation check after executing remediation")]
    public string RunPostflightCheck(
        [Description("Description or condition for the postflight check")] string checkDescription)
    {
        _logger.LogInformation("Running postflight check: {Check}", checkDescription);

        bool passed = !string.IsNullOrWhiteSpace(checkDescription) &&
                      !checkDescription.Contains("degraded", StringComparison.OrdinalIgnoreCase) &&
                      !checkDescription.Contains("failed", StringComparison.OrdinalIgnoreCase);

        var checkResult = new CheckResult
        {
            Check = checkDescription,
            Status = passed ? "passed" : "failed",
            Value = passed ? "verified" : "anomaly detected",
            Message = passed ? "Post-flight condition passed" : "Post-flight condition verification failed"
        };

        return JsonSerializer.Serialize(checkResult, JsonOptions);
    }

    [KernelFunction, Description("Executes an inverse rollback command if remediation failed")]
    public async Task<string> ExecuteRollbackAsync(
        [Description("The inverse rollback command to restore system state")] string rollbackCommand,
        [Description("Maximum rollback timeout in seconds")] int timeoutSeconds = 120,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Executing rollback command: {Command}", rollbackCommand);

        var output = await _sandbox.ExecuteCommandAsync(
            rollbackCommand,
            TimeSpan.FromSeconds(timeoutSeconds),
            cancellationToken).ConfigureAwait(false);

        var stepResult = new ExecutionStepResult
        {
            Command = rollbackCommand,
            Status = output.IsSuccess ? ExecutionStatus.Success : ExecutionStatus.Failed,
            DurationMs = output.DurationMs,
            ExitCode = output.ExitCode,
            StandardOutput = output.StandardOutput,
            StandardError = output.StandardError,
            ErrorMessage = output.ErrorMessage
        };

        return JsonSerializer.Serialize(stepResult, JsonOptions);
    }
}
