using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trinetra.Agents.Coordinator;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbIncidentCoordinatorStore : IIncidentCoordinatorStore
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbIncidentCoordinatorStore> _logger;
    private readonly IIncidentEventNotifier? _notifier;

    public DbIncidentCoordinatorStore(
        ApplicationDbContext dbContext,
        ILogger<DbIncidentCoordinatorStore> logger,
        IIncidentEventNotifier? notifier = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notifier = notifier;
    }

    public async Task<Incident> CreateIncidentAsync(Incident incident, CancellationToken cancellationToken = default)
    {
        _dbContext.Incidents.Add(incident);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_notifier != null)
        {
            try
            {
                await _notifier.NotifyIncidentCreatedAsync(
                    incident.Id,
                    incident.Title,
                    incident.Severity,
                    incident.Status,
                    incident.TriggeredAt,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch real-time incident_created notification for {IncidentId}", incident.Id);
            }
        }

        return incident;
    }

    public async Task<Incident?> GetIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Incidents
            .Include(i => i.Events)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
    }

    public async Task<bool> UpdateIncidentStatusAsync(Guid incidentId, string newStatus, string? reason = null, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
        if (incident == null) return false;

        incident.Status = newStatus;
        incident.UpdatedAt = DateTime.UtcNow;

        if (newStatus == IncidentStatuses.Investigating && incident.StartedInvestigatingAt == null)
        {
            incident.StartedInvestigatingAt = DateTime.UtcNow;
        }
        else if (newStatus == IncidentStatuses.AnalysisComplete && incident.AnalysisCompletedAt == null)
        {
            incident.AnalysisCompletedAt = DateTime.UtcNow;
        }
        else if (newStatus == IncidentStatuses.Executing && incident.RemediationStartedAt == null)
        {
            incident.RemediationStartedAt = DateTime.UtcNow;
        }
        else if (newStatus == IncidentStatuses.Resolved && incident.ResolvedAt == null)
        {
            incident.ResolvedAt = DateTime.UtcNow;
        }
        else if (newStatus == IncidentStatuses.Escalated && incident.EscalatedAt == null)
        {
            incident.EscalatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated incident {IncidentId} status to {Status}", incidentId, newStatus);

        if (_notifier != null)
        {
            try
            {
                await _notifier.NotifyIncidentStatusChangedAsync(
                    incidentId,
                    newStatus,
                    incident.UpdatedAt,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch real-time incident_status_changed notification for {IncidentId}", incidentId);
            }
        }

        return true;
    }

    public async Task<bool> EscalateIncidentAsync(Guid incidentId, string reason, CancellationToken cancellationToken = default)
    {
        return await UpdateIncidentStatusAsync(incidentId, IncidentStatuses.Escalated, reason, cancellationToken);
    }

    public async Task<bool> UpdateIncidentFindingsAsync(
        Guid incidentId,
        string? rootCause,
        string? rootCauseCategory,
        decimal? confidence,
        string? proposedRemediation,
        string? riskLevel,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
        if (incident == null) return false;

        if (rootCause != null) incident.RootCause = rootCause;
        if (rootCauseCategory != null) incident.RootCauseCategory = rootCauseCategory;
        if (confidence.HasValue) incident.RootCauseConfidence = confidence.Value;
        if (proposedRemediation != null) incident.ProposedRemediation = proposedRemediation;
        if (riskLevel != null) incident.RemediationRiskLevel = riskLevel;

        incident.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
