using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Trinetra.Core.Interfaces;

namespace Trinetra.Infrastructure.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly ILogger<EmbeddingService> _logger;
    private const int Dimensions = 1536;

    public EmbeddingService(ILogger<EmbeddingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new float[Dimensions]);
        }

        // Semantic hash-based deterministic projection to 1536-dimensional normalized unit vector
        var vector = new float[Dimensions];
        var clean = text.Trim().ToLowerInvariant();
        var words = clean.Split(new[] { ' ', '\t', '\r', '\n', ',', '.', ';', ':', '-', '_', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(word));
            for (int i = 0; i < Dimensions; i++)
            {
                int byteIdx = (i * 3) % hash.Length;
                int sign = (hash[byteIdx] & 1) == 0 ? 1 : -1;
                float weight = (hash[(byteIdx + 1) % hash.Length] / 255.0f);
                vector[i] += sign * weight;
            }
        }

        // Normalize vector to unit length (L2 norm) for cosine distance
        double sumSquares = 0.0;
        for (int i = 0; i < Dimensions; i++)
        {
            sumSquares += vector[i] * vector[i];
        }

        var norm = (float)Math.Sqrt(sumSquares);
        if (norm > 0)
        {
            for (int i = 0; i < Dimensions; i++)
            {
                vector[i] /= norm;
            }
        }

        return Task.FromResult(vector);
    }
}
