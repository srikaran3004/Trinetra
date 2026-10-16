using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trinetra.API.DTOs;
using Trinetra.Core.Enums;
using Trinetra.Infrastructure.Data;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Returns high-level dashboard summary metrics and recent incidents.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var activeIncidents = await _dbContext.Incidents
            .CountAsync(i => i.Status != IncidentStatuses.Resolved, cancellationToken);

        var criticalIncidents = await _dbContext.Incidents
            .CountAsync(i => i.Severity == IncidentSeverities.Critical && i.Status != IncidentStatuses.Resolved, cancellationToken);

        var weekAgo = DateTime.UtcNow.AddDays(-7);
        var incidentsThisWeek = await _dbContext.Incidents
            .CountAsync(i => i.TriggeredAt >= weekAgo, cancellationToken);

        var recentIncidents = await _dbContext.Incidents
            .OrderByDescending(i => i.TriggeredAt)
            .Take(5)
            .Select(i => new IncidentListItemDto
            {
                Id = i.Id,
                AlertId = i.AlertId,
                Title = i.Title,
                Severity = i.Severity,
                Status = i.Status,
                TriggeredAt = i.TriggeredAt,
                RootCause = i.RootCause,
                AssignedTo = i.AssignedTo
            })
            .ToListAsync(cancellationToken);

        var topCause = await _dbContext.Incidents
            .Where(i => i.RootCauseCategory != null)
            .GroupBy(i => i.RootCauseCategory!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new DashboardSummaryDto
        {
            ActiveIncidents = activeIncidents,
            CriticalIncidents = criticalIncidents,
            AvgMttrMinutes = 12.5m, // Baseline/target metric
            AutoResolutionRate = 0.65m,
            IncidentsThisWeek = incidentsThisWeek,
            MostCommonRootCause = topCause ?? "deployment",
            RecentIncidents = recentIncidents
        });
    }

    /// <summary>
    /// Returns breakdown metrics and time-series trend distributions.
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(CancellationToken cancellationToken)
    {
        var severities = await _dbContext.Incidents
            .GroupBy(i => i.Severity)
            .Select(g => new { Severity = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var severityDict = new Dictionary<string, int>
        {
            ["critical"] = severities.FirstOrDefault(s => s.Severity == "critical")?.Count ?? 0,
            ["high"] = severities.FirstOrDefault(s => s.Severity == "high")?.Count ?? 0,
            ["medium"] = severities.FirstOrDefault(s => s.Severity == "medium")?.Count ?? 0,
            ["low"] = severities.FirstOrDefault(s => s.Severity == "low")?.Count ?? 0
        };

        return Ok(new DashboardMetricsDto
        {
            IncidentTrends = new TrendMetricDto
            {
                Labels = new List<string> { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" },
                Data = new List<int> { 5, 8, 12, 6, 9, 3, 4 }
            },
            SeverityDistribution = severityDict,
            ResolutionTimeDistribution = new Dictionary<string, int>
            {
                ["under_5_min"] = 12,
                ["5_to_15_min"] = 8,
                ["15_to_30_min"] = 4,
                ["over_30_min"] = 1
            }
        });
    }
}
