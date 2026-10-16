using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbMetricsSnapshotRepository : IMetricsSnapshotRepository
{
    private readonly ApplicationDbContext _context;

    public DbMetricsSnapshotRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<MetricsSnapshot> SaveSnapshotAsync(MetricsSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        if (snapshot.Id == Guid.Empty)
        {
            snapshot.Id = Guid.NewGuid();
        }

        var existing = await _context.MetricsSnapshots
            .FirstOrDefaultAsync(s => s.IncidentId == snapshot.IncidentId, cancellationToken)
            .ConfigureAwait(false);

        if (existing != null)
        {
            existing.Source = snapshot.Source;
            existing.QueryExecuted = snapshot.QueryExecuted;
            existing.TimeRangeStart = snapshot.TimeRangeStart;
            existing.TimeRangeEnd = snapshot.TimeRangeEnd;
            existing.MetricsData = snapshot.MetricsData;
            existing.MetricNames = snapshot.MetricNames;
            existing.AnomaliesDetected = snapshot.AnomaliesDetected;
            existing.ResourceSaturation = snapshot.ResourceSaturation;
            existing.BaselineDeviations = snapshot.BaselineDeviations;
            existing.AnalyzedAt = snapshot.AnalyzedAt ?? DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return existing;
        }

        await _context.MetricsSnapshots.AddAsync(snapshot, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    public async Task<MetricsSnapshot?> GetSnapshotByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _context.MetricsSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IncidentId == incidentId, cancellationToken)
            .ConfigureAwait(false);
    }
}
