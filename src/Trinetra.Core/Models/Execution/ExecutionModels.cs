using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Trinetra.Core.Models.Execution;

public static class ExecutionStatus
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Blocked = "blocked";
    public const string Escalated = "escalated";
    public const string RolledBack = "rolled_back";
    public const string Timeout = "timeout";
}

public class CheckResult
{
    [JsonPropertyName("check")]
    public string Check { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "passed"; // "passed" or "failed"

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public class ExecutionStepResult
{
    [JsonPropertyName("step")]
    public int Step { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = ExecutionStatus.Success;

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; }

    [JsonPropertyName("exit_code")]
    public int? ExitCode { get; set; }

    [JsonPropertyName("standard_output")]
    public string? StandardOutput { get; set; }

    [JsonPropertyName("standard_error")]
    public string? StandardError { get; set; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }
}

public class RemediationExecutionResult
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; set; }

    [JsonPropertyName("remediation_id")]
    public string? RemediationId { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = ExecutionStatus.Success;

    [JsonPropertyName("preflight_checks")]
    public List<CheckResult> PreflightChecks { get; set; } = new();

    [JsonPropertyName("execution_steps")]
    public List<ExecutionStepResult> ExecutionSteps { get; set; } = new();

    [JsonPropertyName("postflight_checks")]
    public List<CheckResult> PostflightChecks { get; set; } = new();

    [JsonPropertyName("rollback_executed")]
    public bool RollbackExecuted { get; set; }

    [JsonPropertyName("rollback_success")]
    public bool RollbackSuccess { get; set; }

    [JsonPropertyName("rollback_command")]
    public string? RollbackCommand { get; set; }

    [JsonPropertyName("total_duration_seconds")]
    public int TotalDurationSeconds { get; set; }

    [JsonPropertyName("outcome")]
    public string Outcome => Status == ExecutionStatus.Success ? "SUCCESS" : "FAILED";

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("executed_at")]
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

public class CommandExecutionRequest
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; set; }

    [JsonPropertyName("remediation_id")]
    public string? RemediationId { get; set; }

    [JsonPropertyName("action_title")]
    public string ActionTitle { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("steps")]
    public List<string> Steps { get; set; } = new();

    [JsonPropertyName("rollback_command")]
    public string? RollbackCommand { get; set; }

    [JsonPropertyName("preflight_checks")]
    public List<string> PreflightChecks { get; set; } = new();

    [JsonPropertyName("postflight_checks")]
    public List<string> PostflightChecks { get; set; } = new();

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; set; } = "low";

    [JsonPropertyName("is_approved")]
    public bool IsApproved { get; set; }

    [JsonPropertyName("timeout_seconds")]
    public int TimeoutSeconds { get; set; } = 300;
}
