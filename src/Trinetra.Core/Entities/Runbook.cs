using Pgvector;

namespace Trinetra.Core.Entities;

public class Runbook
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Metadata
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public List<string> Tags { get; set; } = new();

    // Applicability
    public List<string> ApplicableServices { get; set; } = new();
    public List<string> ApplicableHosts { get; set; } = new();
    public List<string> ApplicableErrorPatterns { get; set; } = new();

    // Content
    public string Content { get; set; } = string.Empty; // Markdown format
    public string? RemediationSteps { get; set; } // Structured steps stored as JSONB
    public int? EstimatedTimeMinutes { get; set; }
    public List<string> Prerequisites { get; set; } = new();
    public List<string> RisksAndWarnings { get; set; } = new();

    // Vector Embedding for RAG (nullable per Decision D-11)
    public Vector? ContentEmbedding { get; set; }

    // Versioning
    public int Version { get; set; } = 1;
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public bool IsDeprecated { get; set; } = false;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? CreatedByUser { get; set; }
    public User? UpdatedByUser { get; set; }
}
