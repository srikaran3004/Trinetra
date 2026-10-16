using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trinetra.Core.Interfaces;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
