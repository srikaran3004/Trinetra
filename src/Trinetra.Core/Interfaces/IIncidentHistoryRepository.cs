using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface IIncidentHistoryRepository
{
    Task<IncidentHistory> SaveIncidentHistoryAsync(
        IncidentHistory history,
        CancellationToken cancellationToken = default);

    Task<IncidentHistory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IncidentHistory?> GetByOriginalIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentHistory>> SearchSimilarIncidentsAsync(
        float[] queryEmbedding,
        int limit = 5,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentHistory>> GetRecentAsync(
        int limit = 20,
        CancellationToken cancellationToken = default);
}
