using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Models.Logs;

namespace Trinetra.Agents.LogAnalyzer;

public class LogAnalyzerPlugin
{
    private readonly ILogQueryService _logQueryService;
    private readonly ILogger<LogAnalyzerPlugin> _logger;

    public LogAnalyzerPlugin(ILogQueryService logQueryService, ILogger<LogAnalyzerPlugin> logger)
    {
        _logQueryService = logQueryService ?? throw new ArgumentNullException(nameof(logQueryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction("query_logs")]
    [Description("Queries log aggregation systems (Elasticsearch) for a specific service and time range")]
    public async Task<string> QueryLogsAsync(
        [Description("The target service name to query logs for")] string serviceName,
        [Description("Time range window, e.g. '5m', '15m', '1h'")] string timeRange = "15m",
        [Description("Maximum number of log lines to retrieve (max 10000)")] int limit = 1000,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool query_logs invoked for service '{Service}', range '{Range}', limit {Limit}",
            serviceName, timeRange, limit);

        var result = await _logQueryService.QueryLogsAsync(serviceName, timeRange, null, limit, cancellationToken)
            .ConfigureAwait(false);

        return JsonSerializer.Serialize(new
        {
            service = serviceName,
            time_range = timeRange,
            count = result.TotalCount,
            errors = result.ErrorCount,
            logs = result.Logs.Select(l => new
            {
                timestamp = l.Timestamp.ToString("O"),
                level = l.Level,
                message = l.Message,
                service = l.Service,
                trace_id = l.TraceId,
                stack_trace = l.StackTrace
            }).Take(100) // return top 100 in tool response to respect token budgets
        });
    }

    [KernelFunction("extract_stack_traces")]
    [Description("Parses stack traces, exception types, and programming languages from log text or JSON")]
    public string ExtractStackTraces(
        [Description("Raw log text or JSON string containing logs")] string logData)
    {
        _logger.LogInformation("Tool extract_stack_traces invoked");

        var result = new StackTraceExtractionResult();
        if (string.IsNullOrWhiteSpace(logData))
        {
            return JsonSerializer.Serialize(result);
        }

        var traces = new HashSet<string>();
        var exceptions = new HashSet<string>();
        var languages = new HashSet<string>();

        // Regex patterns for stack traces across languages
        // .NET: "   at Namespace.Class.Method(Args) in path:line 123"
        var dotNetRegex = new Regex(@"(?:at\s+[\w\.<>]+\([^\)]*\)(?:\s+in\s+[^:]+:line\s+\d+)?)", RegexOptions.Multiline);
        // Java: "   at package.Class.method(Class.java:123)"
        var javaRegex = new Regex(@"(?:at\s+[\w\.$]+\([A-Za-z0-9_]+\.java:\d+\))", RegexOptions.Multiline);
        // Python: "  File \"...\", line 123, in method"
        var pythonRegex = new Regex(@"(?:File\s+""[^""]+"",\s+line\s+\d+,\s+in\s+[\w<>]+)", RegexOptions.Multiline);
        // Node.js: "    at Object.method (/path/file.js:12:34)"
        var nodeRegex = new Regex(@"(?:at\s+(?:async\s+)?[\w\.<>]+\s+\([^\)]+\.js:\d+:\d+\))", RegexOptions.Multiline);
        // Generic exception type pattern e.g. "System.NullReferenceException: Object reference..." or "NullPointerException"
        var exceptionRegex = new Regex(@"([A-Z][a-zA-Z0-9_]*(?:Exception|Error))(?::\s*([^\r\n]+))?");

        // Try extracting from JSON array if structured
        var logLines = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(logData);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.TryGetProperty("stack_trace", out var st) && st.ValueKind == JsonValueKind.String)
                        logLines.Add(st.GetString()!);
                    if (item.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        logLines.Add(m.GetString()!);
                }
            }
            else if (doc.RootElement.TryGetProperty("logs", out var logsArr) && logsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in logsArr.EnumerateArray())
                {
                    if (item.TryGetProperty("stack_trace", out var st) && st.ValueKind == JsonValueKind.String)
                        logLines.Add(st.GetString()!);
                    if (item.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
                        logLines.Add(m.GetString()!);
                }
            }
            else
            {
                logLines.Add(logData);
            }
        }
        catch
        {
            logLines.Add(logData);
        }

        foreach (var text in logLines)
        {
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Check exceptions
            var excMatches = exceptionRegex.Matches(text);
            foreach (Match m in excMatches)
            {
                exceptions.Add(m.Value.Trim());
            }

            // Check .NET
            var dnMatches = dotNetRegex.Matches(text);
            if (dnMatches.Count > 0)
            {
                languages.Add("C# / .NET");
                traces.Add(string.Join("\n", dnMatches.Select(m => m.Value.Trim())));
            }

            // Check Java
            var jMatches = javaRegex.Matches(text);
            if (jMatches.Count > 0)
            {
                languages.Add("Java");
                traces.Add(string.Join("\n", jMatches.Select(m => m.Value.Trim())));
            }

            // Check Python
            var pyMatches = pythonRegex.Matches(text);
            if (pyMatches.Count > 0)
            {
                languages.Add("Python");
                traces.Add(string.Join("\n", pyMatches.Select(m => m.Value.Trim())));
            }

            // Check Node.js
            var nodeMatches = nodeRegex.Matches(text);
            if (nodeMatches.Count > 0)
            {
                languages.Add("Node.js / TypeScript");
                traces.Add(string.Join("\n", nodeMatches.Select(m => m.Value.Trim())));
            }
        }

        result.StackTraces = traces.Take(10).ToList();
        result.Exceptions = exceptions.Take(15).ToList();
        result.Languages = languages.ToList();

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("detect_anomalies")]
    [Description("Analyzes log error frequency, bursts, and clusters to detect anomalous behavior and severity")]
    public string DetectAnomalies(
        [Description("Raw log text or JSON string containing logs")] string logData,
        [Description("Optional baseline metrics JSON or description")] string? baselineMetrics = null)
    {
        _logger.LogInformation("Tool detect_anomalies invoked");

        var result = new LogAnomalyDetectionResult();
        if (string.IsNullOrWhiteSpace(logData))
        {
            return JsonSerializer.Serialize(result);
        }

        var patterns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var distribution = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Information", 0 },
            { "Warning", 0 },
            { "Error", 0 },
            { "Critical", 0 }
        };

        var entries = new List<(string level, string message)>();

        try
        {
            using var doc = JsonDocument.Parse(logData);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var lvl = item.TryGetProperty("level", out var l) ? l.GetString() ?? "Information" : "Information";
                    var msg = item.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
                    entries.Add((lvl, msg));
                }
            }
            else if (doc.RootElement.TryGetProperty("logs", out var logsArr) && logsArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in logsArr.EnumerateArray())
                {
                    var lvl = item.TryGetProperty("level", out var l) ? l.GetString() ?? "Information" : "Information";
                    var msg = item.TryGetProperty("message", out var m) ? m.GetString() ?? string.Empty : string.Empty;
                    entries.Add((lvl, msg));
                }
            }
        }
        catch
        {
            // Treat as raw lines
            var lines = logData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var lvl = line.Contains("ERR", StringComparison.OrdinalIgnoreCase) || line.Contains("Exception", StringComparison.OrdinalIgnoreCase)
                    ? "Error"
                    : line.Contains("WARN", StringComparison.OrdinalIgnoreCase)
                        ? "Warning"
                        : "Information";
                entries.Add((lvl, line));
            }
        }

        foreach (var (lvl, msg) in entries)
        {
            var key = lvl.ToLower() switch
            {
                "error" or "err" => "Error",
                "fatal" or "critical" => "Critical",
                "warn" or "warning" => "Warning",
                _ => "Information"
            };

            distribution[key] = distribution.GetValueOrDefault(key, 0) + 1;

            if (key == "Error" || key == "Critical")
            {
                var normalizedPattern = NormalizeLogMessage(msg);
                if (!string.IsNullOrWhiteSpace(normalizedPattern))
                {
                    patterns[normalizedPattern] = patterns.GetValueOrDefault(normalizedPattern, 0) + 1;
                }
            }
        }

        var total = entries.Count;
        var totalErrors = distribution["Error"] + distribution["Critical"];
        var errorRate = total > 0 ? (double)totalErrors / total : 0;

        if (totalErrors > 50 || errorRate > 0.25)
        {
            result.Severity = "critical";
            result.Anomalies.Add($"High error volume: {totalErrors} errors detected ({errorRate:P1} error rate)");
        }
        else if (totalErrors > 10 || errorRate > 0.05)
        {
            result.Severity = "high";
            result.Anomalies.Add($"Elevated error rate: {totalErrors} errors detected ({errorRate:P1} error rate)");
        }
        else if (totalErrors > 0)
        {
            result.Severity = "medium";
            result.Anomalies.Add($"Error detected: {totalErrors} errors in time window");
        }
        else
        {
            result.Severity = "low";
        }

        // Top repeated patterns
        var topPatterns = patterns
            .OrderByDescending(p => p.Value)
            .Take(5)
            .Select(p => $"{p.Key} (occurrences: {p.Value})")
            .ToList();

        if (topPatterns.Count > 0)
        {
            result.Anomalies.Add($"Repeated error cluster identified: {topPatterns.First()}");
        }

        result.ErrorPatterns = topPatterns;
        result.SeverityDistribution = distribution;

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("correlate_with_deployment")]
    [Description("Checks for recent service deployments and changed files around the incident window")]
    public string CorrelateWithDeployment(
        [Description("The service name to correlate")] string service,
        [Description("Time range window, e.g. '1h', '2h'")] string? timeRange = "1h")
    {
        _logger.LogInformation("Tool correlate_with_deployment invoked for service '{Service}'", service);

        // Simulation / query of deployment records
        var result = new DeploymentCorrelationResult
        {
            RecentDeployments = new List<DeploymentInfo>
            {
                new()
                {
                    Service = service,
                    Version = "v1.2.4",
                    DeployedAt = DateTime.UtcNow.AddMinutes(-25),
                    ChangedFiles = new List<string> { $"{service}/Handler.cs", $"{service}/Config.json" }
                }
            },
            ChangedFiles = new List<string> { $"{service}/Handler.cs", $"{service}/Config.json" }
        };

        return JsonSerializer.Serialize(result);
    }

    public static string NormalizeLogMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return string.Empty;

        // Strip timestamps (e.g., 2026-10-16T14:23:45Z)
        var normalized = Regex.Replace(message, @"\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?", "<TIMESTAMP>");
        // Strip GUIDs/UUIDs
        normalized = Regex.Replace(normalized, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", "<UUID>");
        // Strip IP addresses
        normalized = Regex.Replace(normalized, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", "<IP>");
        // Strip hex addresses (0x...)
        normalized = Regex.Replace(normalized, @"0x[0-9a-fA-F]+", "<HEX>");
        // Strip long digit sequences (IDs)
        normalized = Regex.Replace(normalized, @"\b\d{5,}\b", "<ID>");

        return normalized.Trim();
    }
}
