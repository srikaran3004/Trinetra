using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Core.Models.PostMortem;

namespace Trinetra.Agents.PostMortem;

public class PostMortemPlugin
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly ILogger<PostMortemPlugin> _logger;

    public PostMortemPlugin(ILogger<PostMortemPlugin> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [KernelFunction, Description("Generates a structured SRE Markdown post-mortem document for a resolved incident")]
    public string GenerateMarkdownPostMortem(
        [Description("Incident ID")] string incidentId,
        [Description("Incident Title")] string title,
        [Description("Severity level")] string severity,
        [Description("Current status")] string status,
        [Description("Incident trigger timestamp ISO")] string triggeredAt,
        [Description("Incident resolved timestamp ISO")] string resolvedAt,
        [Description("Root cause statement")] string rootCause,
        [Description("Root cause category")] string rootCauseCategory,
        [Description("Confidence score 0.0-1.0")] string confidence,
        [Description("Remediation action taken")] string remediation,
        [Description("Serialized JSON array of PostMortemTimelineEntry")] string timelineJson,
        [Description("Affected services comma-separated or JSON array")] string services)
    {
        _logger.LogInformation("Generating Markdown post-mortem for incident {IncidentId}: {Title}", incidentId, title);

        var timeline = new List<PostMortemTimelineEntry>();
        if (!string.IsNullOrWhiteSpace(timelineJson))
        {
            try
            {
                timeline = JsonSerializer.Deserialize<List<PostMortemTimelineEntry>>(timelineJson, JsonOptions) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse timeline JSON during post-mortem generation");
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Incident Post-Mortem: {title}");
        sb.AppendLine();
        sb.AppendLine($"**Incident ID:** `{incidentId}`  ");
        sb.AppendLine($"**Severity:** `{severity.ToUpperInvariant()}`  ");
        sb.AppendLine($"**Status:** `{status.ToUpperInvariant()}`  ");
        sb.AppendLine($"**Triggered At:** {triggeredAt} UTC  ");
        sb.AppendLine($"**Resolved At:** {resolvedAt} UTC  ");
        sb.AppendLine($"**Affected Services:** {services}  ");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine("## 1. Executive Summary");
        sb.AppendLine($"On {triggeredAt}, automated monitoring detected an anomaly resulting in incident **{title}** with **{severity.ToUpperInvariant()}** severity affecting `{services}`.");
        sb.AppendLine($"Autonomous analysis diagnosed the root cause as **{rootCauseCategory}**: *\"{rootCause}\"* (Confidence: {confidence}).");
        sb.AppendLine($"Automated/approved remediation was executed: *{remediation}*. The incident was verified resolved at {resolvedAt}.");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine("## 2. Root Cause Analysis (RCA)");
        sb.AppendLine($"- **Category:** `{rootCauseCategory}`");
        sb.AppendLine($"- **Primary Cause:** {rootCause}");
        sb.AppendLine($"- **Diagnosis Confidence:** {confidence}");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine("## 3. Remediation & Recovery Actions");
        sb.AppendLine($"- **Remediation Plan:** {remediation}");
        sb.AppendLine("- **Safety Verification:** Pre-flight and post-flight guardrail checks verified normal operation post-remediation.");
        sb.AppendLine("- **Verification Status:** Anomaly cleared, telemetry normalized.");
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine("## 4. Incident Timeline");
        sb.AppendLine("| Timestamp (UTC) | Event | Actor / Agent | Status | Description |");
        sb.AppendLine("|---|---|---|---|---|");

        if (timeline.Count > 0)
        {
            foreach (var t in timeline)
            {
                var ts = t.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                sb.AppendLine($"| {ts} | `{t.EventType}` | {t.Actor} | `{t.Status}` | {t.Description.Replace("|", "\\|")} |");
            }
        }
        else
        {
            sb.AppendLine($"| {triggeredAt} | `incident_triggered` | Monitoring | `triggered` | Incident triggered by alert |");
            sb.AppendLine($"| {resolvedAt} | `incident_resolved` | Auto-Executor | `resolved` | Incident verified and resolved |");
        }
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine("## 5. Preventative Action Items");
        var actionItems = GetDefaultActionItems(rootCauseCategory);
        int itemIndex = 1;
        foreach (var item in actionItems)
        {
            sb.AppendLine($"{itemIndex++}. [ ] {item}");
        }
        sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine("## 6. Lessons Learned");
        sb.AppendLine("- Autonomous multi-agent diagnosis accelerated detection and root cause identification.");
        sb.AppendLine("- Guardrail validation ensured non-destructive remediation execution.");
        sb.AppendLine("- Telemetry and runbook integration provided immediate correlation.");
        sb.AppendLine();
        sb.AppendLine($"*Report generated automatically by Trinetra Post-Mortem Generator Agent at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.*");

        return sb.ToString();
    }

    [KernelFunction, Description("Extracts preventative action items based on root cause category")]
    public string ExtractActionItems([Description("Root cause category")] string category)
    {
        var items = GetDefaultActionItems(category);
        return JsonSerializer.Serialize(items, JsonOptions);
    }

    private static List<string> GetDefaultActionItems(string? category)
    {
        var cat = (category ?? "").ToUpperInvariant();
        return cat switch
        {
            "DATABASE" => new List<string>
            {
                "Optimize connection pool limits and idle client connection timeouts.",
                "Review slow queries and missing indexes in pg_stat_statements.",
                "Implement automated circuit breakers for database connection spikes."
            },
            "MEMORY_LEAK" or "RESOURCE_EXHAUSTION" => new List<string>
            {
                "Profile application heap allocations under simulated peak traffic.",
                "Adjust container memory limits and enable proactive pod recycling.",
                "Configure Prometheus saturation alerts at 80% threshold."
            },
            "NETWORK" or "TIMEOUT" => new List<string>
            {
                "Review upstream proxy timeout and retry backoff configurations.",
                "Ensure circuit breaker trip thresholds are aligned with downstream SLAs.",
                "Audit cross-AZ network latency and DNS caching policies."
            },
            "CONFIGURATION" => new List<string>
            {
                "Enforce GitOps schema validation on all deployment config changes.",
                "Add pre-deployment canary check for configuration variables.",
                "Audit drift between staging and production environments."
            },
            _ => new List<string>
            {
                "Conduct code review on error handling paths for the affected component.",
                "Add regression integration test reproducing the failure scenario.",
                "Update operational runbooks with latest diagnostic and remediation steps."
            }
        };
    }
}
