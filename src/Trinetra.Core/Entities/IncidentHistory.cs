using Pgvector;

namespace Trinetra.Core.Entities;

public class IncidentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Summary
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? RootCause { get; set; }
    public string? RootCauseCategory { get; set; }
    public string? RemediationAction { get; set; }
    public int? RemediationTimeMinutes { get; set; }

    // Classification
    public string? Severity { get; set; }
    public List<string> AffectedServices { get; set; } = new();
    public List<string> Tags { get; set; } = new();

    // Embedding for similarity search
    public Vector? HistoryEmbedding { get; set; }

    // Timeline
    public DateTime? OccurredAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? ResolutionTimeMinutes { get; set; }

    // Reference
    public Guid? OriginalIncidentId { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
