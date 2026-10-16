using System;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Models.Logs;

namespace Trinetra.Core.Interfaces;

public interface ILogQueryService
{
    Task<LogQueryResult> QueryLogsAsync(
        string serviceName,
        string timeRange = "15m",
        string? filter = null,
        int limit = 1000,
        CancellationToken cancellationToken = default);

    Task<LogQueryResult> QueryLogsByTimeRangeAsync(
        string? serviceName,
        DateTime start,
        DateTime end,
        string? filter = null,
        int limit = 1000,
        CancellationToken cancellationToken = default);
}
