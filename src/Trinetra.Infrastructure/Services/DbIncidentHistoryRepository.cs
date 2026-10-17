using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbIncidentHistoryRepository : IIncidentHistoryRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DbIncidentHistoryRepository> _logger;

    public DbIncidentHistoryRepository(
        ApplicationDbContext context,
        ILogger<DbIncidentHistoryRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IncidentHistory> SaveIncidentHistoryAsync(
        IncidentHistory history,
        CancellationToken cancellationToken = default)
    {
        if (history == null) throw new ArgumentNullException(nameof(history));

        if (history.OriginalIncidentId.HasValue)
        {
            var existing = await _context.IncidentHistories
                .FirstOrDefaultAsync(h => h.OriginalIncidentId == history.OriginalIncidentId.Value, cancellationToken)
                .ConfigureAwait(false);

            if (existing != null)
            {
                existing.Title = history.Title;
                existing.Description = history.Description;
                existing.RootCause = history.RootCause;
                existing.RootCauseCategory = history.RootCauseCategory;
                existing.RemediationAction = history.RemediationAction;
                existing.RemediationTimeMinutes = history.RemediationTimeMinutes;
                existing.Severity = history.Severity;
                existing.AffectedServices = history.AffectedServices;
                existing.Tags = history.Tags;
                if (history.HistoryEmbedding != null) existing.HistoryEmbedding = history.HistoryEmbedding;
                existing.OccurredAt = history.OccurredAt;
                existing.ResolvedAt = history.ResolvedAt;
                existing.ResolutionTimeMinutes = history.ResolutionTimeMinutes;

                await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return existing;
            }
        }

        _context.IncidentHistories.Add(history);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return history;
    }

    public async Task<IncidentHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.IncidentHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IncidentHistory?> GetByOriginalIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _context.IncidentHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.OriginalIncidentId == incidentId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IncidentHistory>> SearchSimilarIncidentsAsync(
        float[] queryEmbedding,
        int limit = 5,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding == null || queryEmbedding.Length == 0)
        {
            return await GetRecentAsync(limit, cancellationToken).ConfigureAwait(false);
        }

        limit = Math.Clamp(limit, 1, 20);
        var targetVector = new Vector(queryEmbedding);
        var isNpgsql = _context.Database.IsNpgsql();

        if (isNpgsql)
        {
            try
            {
                return await _context.IncidentHistories
                    .AsNoTracking()
                    .Where(h => h.HistoryEmbedding != null)
                    .OrderBy(h => h.HistoryEmbedding!.CosineDistance(targetVector))
                    .Take(limit)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "pgvector search failed in PostgreSQL, falling back to in-memory cosine scoring");
            }
        }

        // In-memory fallback
        var all = await _context.IncidentHistories
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var scored = all
            .Select(h => new
            {
                History = h,
                Similarity = h.HistoryEmbedding != null
                    ? ComputeCosineSimilarity(queryEmbedding, h.HistoryEmbedding.ToArray())
                    : 0.0
            })
            .OrderByDescending(x => x.Similarity)
            .Take(limit)
            .Select(x => x.History)
            .ToList();

        return scored;
    }

    public async Task<IReadOnlyList<IncidentHistory>> GetRecentAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 100);
        return await _context.IncidentHistories
            .AsNoTracking()
            .OrderByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static double ComputeCosineSimilarity(float[] v1, float[] v2)
    {
        if (v1 == null || v2 == null || v1.Length == 0 || v2.Length == 0) return 0;
        int len = Math.Min(v1.Length, v2.Length);
        double dot = 0, norm1 = 0, norm2 = 0;
        for (int i = 0; i < len; i++)
        {
            dot += v1[i] * v2[i];
            norm1 += v1[i] * v1[i];
            norm2 += v2[i] * v2[i];
        }
        var denominator = Math.Sqrt(norm1) * Math.Sqrt(norm2);
        return denominator == 0 ? 0 : Math.Clamp(dot / denominator, 0.0, 1.0);
    }
}
