using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Logs;

namespace Trinetra.Infrastructure.Services;

public class ElasticsearchLogQueryService : ILogQueryService
{
    private readonly ElasticsearchClient _client;
    private readonly ILogger<ElasticsearchLogQueryService> _logger;
    private readonly string _defaultIndex;

    public ElasticsearchLogQueryService(
        IConfiguration configuration,
        ILogger<ElasticsearchLogQueryService> logger,
        ElasticsearchClient? client = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var esUrl = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        _defaultIndex = configuration["Elasticsearch:LogsIndex"] ?? "trinetra-logs-*";

        if (client != null)
        {
            _client = client;
        }
        else
        {
            var settings = new ElasticsearchClientSettings(new Uri(esUrl))
                .DefaultIndex(_defaultIndex)
                .RequestTimeout(TimeSpan.FromSeconds(10));
            _client = new ElasticsearchClient(settings);
        }
    }

    public async Task<LogQueryResult> QueryLogsAsync(
        string serviceName,
        string timeRange = "15m",
        string? filter = null,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        var duration = ParseTimeRange(timeRange);
        var end = DateTime.UtcNow;
        var start = end - duration;

        return await QueryLogsByTimeRangeAsync(serviceName, start, end, filter, limit, cancellationToken).ConfigureAwait(false);
    }

    public async Task<LogQueryResult> QueryLogsByTimeRangeAsync(
        string? serviceName,
        DateTime start,
        DateTime end,
        string? filter = null,
        int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 10000);

        try
        {
            var searchResponse = await _client.SearchAsync<JsonElement>(s => s
                .Indices(_defaultIndex)
                .From(0)
                .Size(limit)
                .Sort(sort => sort.Field(new Field("@timestamp"), f => f.Order(SortOrder.Desc)))
                .Query(q => q.Bool(b =>
                {
                    var filterQueries = new List<Query>
                    {
                        new DateRangeQuery(new Field("@timestamp"))
                        {
                            Gte = start.ToString("O"),
                            Lte = end.ToString("O")
                        }
                    };

                    if (!string.IsNullOrWhiteSpace(serviceName))
                    {
                        filterQueries.Add(new MultiMatchQuery
                        {
                            Fields = new[] { new Field("service"), new Field("Application"), new Field("service.name") },
                            Query = serviceName
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(filter))
                    {
                        filterQueries.Add(new QueryStringQuery
                        {
                            Query = filter
                        });
                    }

                    b.Filter(filterQueries);
                })), cancellationToken).ConfigureAwait(false);

            if (!searchResponse.IsValidResponse)
            {
                _logger.LogWarning("Elasticsearch search query returned invalid response: {Debug}", searchResponse.DebugInformation);
                return new LogQueryResult
                {
                    Logs = Array.Empty<LogEntry>(),
                    TotalCount = 0,
                    ErrorCount = 0,
                    TimeRangeStart = start,
                    TimeRangeEnd = end
                };
            }

            var entries = new List<LogEntry>();
            foreach (var hit in searchResponse.Documents)
            {
                var entry = ParseJsonLog(hit);
                if (entry != null)
                {
                    entries.Add(entry);
                }
            }

            var errorCount = entries.Count(e =>
                e.Level.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                e.Level.Equals("Fatal", StringComparison.OrdinalIgnoreCase) ||
                e.Level.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(e.StackTrace));

            return new LogQueryResult
            {
                Logs = entries,
                TotalCount = (int)searchResponse.Total,
                ErrorCount = errorCount,
                TimeRangeStart = start,
                TimeRangeEnd = end
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Elasticsearch for service '{ServiceName}'. Returning empty log result.", serviceName);
            return new LogQueryResult
            {
                Logs = Array.Empty<LogEntry>(),
                TotalCount = 0,
                ErrorCount = 0,
                TimeRangeStart = start,
                TimeRangeEnd = end
            };
        }
    }

    private static LogEntry? ParseJsonLog(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        var entry = new LogEntry();

        if (element.TryGetProperty("@timestamp", out var ts) && DateTime.TryParse(ts.GetString(), out var dt))
        {
            entry.Timestamp = dt;
        }

        if (element.TryGetProperty("level", out var lvl) || element.TryGetProperty("log.level", out lvl))
        {
            entry.Level = lvl.GetString() ?? "Information";
        }

        if (element.TryGetProperty("message", out var msg) || element.TryGetProperty("RenderedMessage", out msg))
        {
            entry.Message = msg.GetString() ?? string.Empty;
        }

        if (element.TryGetProperty("service", out var svc) || element.TryGetProperty("Application", out svc))
        {
            entry.Service = svc.GetString();
        }

        if (element.TryGetProperty("host", out var host) || element.TryGetProperty("host.name", out host))
        {
            entry.Host = host.GetString();
        }

        if (element.TryGetProperty("traceId", out var traceId) || element.TryGetProperty("trace.id", out traceId))
        {
            entry.TraceId = traceId.GetString();
        }

        if (element.TryGetProperty("stackTrace", out var stack) || element.TryGetProperty("exception", out stack) || element.TryGetProperty("error.stack_trace", out stack))
        {
            entry.StackTrace = stack.GetString();
        }

        return entry;
    }

    public static TimeSpan ParseTimeRange(string timeRange) => TimeRangeParser.Parse(timeRange);
}
