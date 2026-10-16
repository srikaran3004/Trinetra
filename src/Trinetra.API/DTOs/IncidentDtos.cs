using System.Text.Json.Serialization;

namespace Trinetra.API.DTOs;

public record CreateIncidentRequest
{
    [JsonPropertyName("alert_id")]
    public string AlertId { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = "medium";

    [JsonPropertyName("affected_service")]
    public string? AffectedService { get; init; }

    [JsonPropertyName("affected_services")]
    public List<string>? AffectedServices { get; init; }

    [JsonPropertyName("affected_hosts")]
    public List<string>? AffectedHosts { get; init; }

    [JsonPropertyName("alert_data")]
    public object? AlertData { get; init; }
}

public record CreateIncidentResponse
{
    [JsonPropertyName("incident_id")]
    public Guid IncidentId { get; init; }

    [JsonPropertyName("alert_id")]
    public string AlertId { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

public record IncidentListItemDto
{
    public Guid Id { get; init; }
    public string AlertId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime TriggeredAt { get; init; }
    public string? RootCause { get; init; }
    public Guid? AssignedTo { get; init; }
    public string? AssignedUserName { get; init; }
}

public record IncidentDetailDto
{
    public Guid Id { get; init; }
    public string AlertId { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public List<string> AffectedServices { get; init; } = new();
    public List<string> AffectedHosts { get; init; } = new();
    public string? RootCause { get; init; }
    public decimal? RootCauseConfidence { get; init; }
    public string? ProposedRemediation { get; init; }
    public string? RemediationRiskLevel { get; init; }
    public DateTime TriggeredAt { get; init; }
    public DateTime? StartedInvestigatingAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public Guid? AssignedTo { get; init; }
    public IEnumerable<IncidentEventDto> Events { get; init; } = Enumerable.Empty<IncidentEventDto>();
}

public record UpdateIncidentRequest
{
    [JsonPropertyName("assigned_to")]
    public Guid? AssignedTo { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}

public record IncidentEventDto
{
    public Guid Id { get; init; }
    public Guid IncidentId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? AgentName { get; init; }
    public string? ActionDescription { get; init; }
    public string? ToolCall { get; init; }
    public string? ToolParams { get; init; }
    public string? ToolResult { get; init; }
    public string ExecutionStatus { get; init; } = "pending";
    public bool GuardrailTriggered { get; init; }
    public bool RequiresApproval { get; init; }
    public DateTime Timestamp { get; init; }
}
