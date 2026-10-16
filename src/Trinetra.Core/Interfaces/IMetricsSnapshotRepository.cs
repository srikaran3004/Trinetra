using System;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface IMetricsSnapshotRepository
{
    Task<MetricsSnapshot> SaveSnapshotAsync(MetricsSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<MetricsSnapshot?> GetSnapshotByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
}
