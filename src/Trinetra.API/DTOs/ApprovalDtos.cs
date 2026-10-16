using System.Text.Json.Serialization;

namespace Trinetra.API.DTOs;

public record PendingApprovalDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; init; }

    [JsonPropertyName("action")]
    public string Action { get; init; } = string.Empty;

    [JsonPropertyName("risk_level")]
    public string RiskLevel { get; init; } = "medium";

    [JsonPropertyName("approval_level")]
    public string ApprovalLevel { get; init; } = "lead";

    [JsonPropertyName("timeout_at")]
    public DateTime? TimeoutAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

public record ApproveRequest
{
    [JsonPropertyName("approved_by_user_id")]
    public Guid? ApprovedByUserId { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("scheduled_for")]
    public DateTime? ScheduledFor { get; init; }
}

public record ApproveResponse
{
    [JsonPropertyName("approval_id")]
    public Guid ApprovalId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "approved";

    [JsonPropertyName("approved_at")]
    public DateTime ApprovedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("remediation_will_execute_at")]
    public DateTime? RemediationWillExecuteAt { get; init; }
}

public record DenyRequest
{
    [JsonPropertyName("denied_by_user_id")]
    public Guid? DeniedByUserId { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}

public record DenyResponse
{
    [JsonPropertyName("approval_id")]
    public Guid ApprovalId { get; init; }

    [JsonPropertyName("status")]
    public string Status { get; init; } = "denied";

    [JsonPropertyName("denied_at")]
    public DateTime DeniedAt { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("incident_status")]
    public string IncidentStatus { get; init; } = "escalated";
}
