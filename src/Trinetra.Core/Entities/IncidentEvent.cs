namespace Trinetra.Core.Entities;

public class IncidentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }

    // Event Classification
    public string EventType { get; set; } = string.Empty;
    public string? AgentName { get; set; }

    // Event Details
    public string? ActionDescription { get; set; }
    public string? ToolCall { get; set; }
    public string? ToolParams { get; set; } // Stored as JSONB
    public string? ToolResult { get; set; }

    // Execution Status
    public string ExecutionStatus { get; set; } = "pending";
    public string? ExecutionError { get; set; }
    public int? ExecutionTimeMs { get; set; }

    // Guardrails & Safety
    public bool GuardrailTriggered { get; set; } = false;
    public string? GuardrailName { get; set; }
    public string? GuardrailReason { get; set; }

    // Approval Info
    public bool RequiresApproval { get; set; } = false;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovalTimestamp { get; set; }
    public string? ApprovalNotes { get; set; }
    public int? ApprovalTimeoutMinutes { get; set; }

    // Audit Trail
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Incident Incident { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public User? CreatedByUser { get; set; }
}
