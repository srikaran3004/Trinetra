using System;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface ILogsSnapshotRepository
{
    Task<LogsSnapshot> SaveSnapshotAsync(LogsSnapshot snapshot, CancellationToken cancellationToken = default);
    Task<LogsSnapshot?> GetSnapshotByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
}
