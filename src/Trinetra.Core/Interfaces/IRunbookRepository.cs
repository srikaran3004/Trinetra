using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface IRunbookRepository
{
    Task<List<(Runbook Runbook, double SimilarityScore)>> SearchByEmbeddingAsync(
        float[] embedding,
        int topK = 5,
        double maxCosineDistance = 0.5,
        CancellationToken cancellationToken = default);

    Task<List<(Runbook Runbook, double MatchScore)>> SearchByKeywordsAsync(
        IEnumerable<string> keywords,
        int topK = 5,
        CancellationToken cancellationToken = default);

    Task<List<(IncidentHistory History, double SimilarityScore)>> SearchIncidentHistoryAsync(
        float[]? embedding,
        string? queryText = null,
        int topK = 3,
        CancellationToken cancellationToken = default);

    Task<Runbook?> GetRunbookByIdAsync(Guid runbookId, CancellationToken cancellationToken = default);
}
