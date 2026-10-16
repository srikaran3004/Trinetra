using System.Text.Json.Serialization;

namespace Trinetra.API.DTOs;

public record CreateRunbookRequest
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("tags")]
    public List<string>? Tags { get; init; }

    [JsonPropertyName("applicable_services")]
    public List<string>? ApplicableServices { get; init; }

    [JsonPropertyName("content")]
    public string Content { get; init; } = string.Empty;

    [JsonPropertyName("remediation_steps")]
    public object? RemediationSteps { get; init; }

    [JsonPropertyName("estimated_time_minutes")]
    public int? EstimatedTimeMinutes { get; init; }
}

public record RunbookListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Category { get; init; }
    public List<string> Tags { get; init; } = new();
    public List<string> ApplicableServices { get; init; } = new();
    public int? EstimatedTimeMinutes { get; init; }
    public bool IsActive { get; init; }
    public int Version { get; init; }
}

public record RunbookDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Category { get; init; }
    public List<string> Tags { get; init; } = new();
    public List<string> ApplicableServices { get; init; } = new();
    public string Content { get; init; } = string.Empty;
    public string? RemediationSteps { get; init; }
    public int? EstimatedTimeMinutes { get; init; }
    public int Version { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
