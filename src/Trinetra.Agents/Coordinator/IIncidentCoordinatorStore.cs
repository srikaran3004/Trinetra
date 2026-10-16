using Trinetra.Core.Entities;

namespace Trinetra.Agents.Coordinator;

public interface IIncidentCoordinatorStore
{
    Task<Incident> CreateIncidentAsync(Incident incident, CancellationToken cancellationToken = default);
    Task<Incident?> GetIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<bool> UpdateIncidentStatusAsync(Guid incidentId, string newStatus, string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> EscalateIncidentAsync(Guid incidentId, string reason, CancellationToken cancellationToken = default);
    Task<bool> UpdateIncidentFindingsAsync(Guid incidentId, string? rootCause, string? rootCauseCategory, decimal? confidence, string? proposedRemediation, string? riskLevel, CancellationToken cancellationToken = default);
}
