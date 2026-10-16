using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Models.Metrics;

namespace Trinetra.Core.Interfaces;

public interface IPrometheusMetricsService
{
    Task<MetricQueryResult> QueryMetricsAsync(
        IEnumerable<string> metricNames,
        string timeRange = "15m",
        string aggregation = "avg",
        CancellationToken cancellationToken = default);

    Task<MetricBaselineResult> GetBaselineMetricsAsync(
        string service,
        string metric,
        int lookbackDays = 7,
        CancellationToken cancellationToken = default);

    Task<string> QueryInstantRawAsync(
        string query,
        DateTime? time = null,
        CancellationToken cancellationToken = default);

    Task<string> QueryRangeRawAsync(
        string query,
        DateTime start,
        DateTime end,
        string step = "15s",
        CancellationToken cancellationToken = default);
}
