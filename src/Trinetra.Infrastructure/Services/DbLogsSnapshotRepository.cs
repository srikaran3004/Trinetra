using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbLogsSnapshotRepository : ILogsSnapshotRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbLogsSnapshotRepository> _logger;

    public DbLogsSnapshotRepository(ApplicationDbContext dbContext, ILogger<DbLogsSnapshotRepository> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LogsSnapshot> SaveSnapshotAsync(LogsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        _dbContext.LogsSnapshots.Add(snapshot);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Saved LogsSnapshot {SnapshotId} for incident {IncidentId} with {Count} logs",
            snapshot.Id, snapshot.IncidentId, snapshot.LogCount);

        return snapshot;
    }

    public async Task<LogsSnapshot?> GetSnapshotByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LogsSnapshots
            .AsNoTracking()
            .OrderByDescending(s => s.RetrievedAt)
            .FirstOrDefaultAsync(s => s.IncidentId == incidentId, cancellationToken)
            .ConfigureAwait(false);
    }
}
