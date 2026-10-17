using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class ApprovalWorkflowService : IApprovalWorkflowService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IIncidentEventNotifier? _notifier;
    private readonly ILogger<ApprovalWorkflowService> _logger;

    public ApprovalWorkflowService(
        ApplicationDbContext dbContext,
        ILogger<ApprovalWorkflowService> logger,
        IIncidentEventNotifier? notifier = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notifier = notifier;
    }

    public async Task<IncidentEvent> CreateApprovalRequestAsync(
        Guid incidentId,
        string actionDescription,
        string? toolCall,
        string riskLevel,
        string approvalLevel = "lead",
        int timeoutMinutes = 15,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
        if (incident == null)
        {
            throw new InvalidOperationException($"Incident {incidentId} not found");
        }

        // 1. Transition incident state to AwaitingApproval
        incident.Status = IncidentStatuses.AwaitingApproval;
        incident.UpdatedAt = DateTime.UtcNow;

        // 2. Create pending approval event in incident_events
        var approvalEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "approval_required",
            AgentName = "ApprovalGate",
            ActionDescription = actionDescription,
            ToolCall = toolCall,
            ExecutionStatus = ExecutionStatuses.Pending,
            RequiresApproval = true,
            ApprovalTimeoutMinutes = timeoutMinutes,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(approvalEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created approval request {ApprovalId} for incident {IncidentId}: Action='{Action}', Risk={Risk}, Level={Level}",
            approvalEvent.Id, incidentId, actionDescription, riskLevel, approvalLevel);

        // 3. Dispatch SignalR notifications
        if (_notifier != null)
        {
            try
            {
                await _notifier.NotifyApprovalRequiredAsync(
                    incidentId,
                    approvalEvent.Id,
                    actionDescription,
                    toolCall,
                    timeoutMinutes,
                    cancellationToken).ConfigureAwait(false);

                await _notifier.NotifyIncidentStatusChangedAsync(
                    incidentId,
                    IncidentStatuses.AwaitingApproval,
                    DateTimeOffset.UtcNow,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast SignalR events for approval request {ApprovalId}", approvalEvent.Id);
            }
        }

        return approvalEvent;
    }

    public async Task<bool> ApproveAsync(
        Guid approvalId,
        Guid? approvedByUserId,
        string? notes,
        DateTime? scheduledFor = null,
        CancellationToken cancellationToken = default)
    {
        var evt = await _dbContext.IncidentEvents
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == approvalId, cancellationToken);

        if (evt == null)
        {
            _logger.LogWarning("Approval request {ApprovalId} not found", approvalId);
            return false;
        }

        evt.ApprovedBy = approvedByUserId;
        evt.ApprovalTimestamp = DateTime.UtcNow;
        evt.ApprovalNotes = notes;
        evt.ExecutionStatus = ExecutionStatuses.Success;

        var incident = evt.Incident;
        if (incident != null)
        {
            incident.Status = IncidentStatuses.Executing;
            incident.RemediationStartedAt ??= DateTime.UtcNow;
            incident.UpdatedAt = DateTime.UtcNow;
        }

        // Ground Rule: Every agent/user action writes to incident_events
        var auditEvent = new IncidentEvent
        {
            IncidentId = evt.IncidentId,
            EventType = "remediation_approved",
            AgentName = "ApprovalGate",
            ActionDescription = $"Remediation action '{evt.ActionDescription}' approved by user {approvedByUserId}. Transitioning incident to '{IncidentStatuses.Executing}'. Notes: {notes}",
            ExecutionStatus = ExecutionStatuses.Success,
            CreatedByUserId = approvedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Approval {ApprovalId} approved. Incident {IncidentId} status transitioned to {Status}",
            approvalId, evt.IncidentId, IncidentStatuses.Executing);

        if (_notifier != null && incident != null)
        {
            try
            {
                await _notifier.NotifyIncidentStatusChangedAsync(
                    incident.Id,
                    IncidentStatuses.Executing,
                    DateTimeOffset.UtcNow,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast status change for approved incident {IncidentId}", incident.Id);
            }
        }

        return true;
    }

    public async Task<bool> DenyAsync(
        Guid approvalId,
        Guid? deniedByUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var evt = await _dbContext.IncidentEvents
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == approvalId, cancellationToken);

        if (evt == null)
        {
            _logger.LogWarning("Approval request {ApprovalId} not found", approvalId);
            return false;
        }

        evt.ApprovalTimestamp = DateTime.UtcNow;
        evt.ApprovalNotes = $"Denied: {reason}";
        evt.ExecutionStatus = ExecutionStatuses.Failed;

        var incident = evt.Incident;
        if (incident != null)
        {
            incident.Status = IncidentStatuses.Escalated;
            incident.EscalatedAt ??= DateTime.UtcNow;
            incident.UpdatedAt = DateTime.UtcNow;
        }

        // Ground Rule: Every agent/user action writes to incident_events
        var auditEvent = new IncidentEvent
        {
            IncidentId = evt.IncidentId,
            EventType = "remediation_denied",
            AgentName = "ApprovalGate",
            ActionDescription = $"Remediation action '{evt.ActionDescription}' denied by user {deniedByUserId}. Reason: {reason}. Incident marked Escalated.",
            ExecutionStatus = ExecutionStatuses.Failed,
            CreatedByUserId = deniedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Approval {ApprovalId} denied. Incident {IncidentId} escalated.", approvalId, evt.IncidentId);

        if (_notifier != null && incident != null)
        {
            try
            {
                await _notifier.NotifyIncidentStatusChangedAsync(
                    incident.Id,
                    IncidentStatuses.Escalated,
                    DateTimeOffset.UtcNow,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast status change for denied incident {IncidentId}", incident.Id);
            }
        }

        return true;
    }

    public async Task<bool> ApproveByIncidentIdAsync(
        Guid incidentId,
        Guid? approvedByUserId,
        string? notes,
        DateTime? scheduledFor = null,
        CancellationToken cancellationToken = default)
    {
        var pendingApproval = await _dbContext.IncidentEvents
            .Where(e => e.IncidentId == incidentId && e.RequiresApproval && e.ApprovalTimestamp == null && e.ExecutionStatus == ExecutionStatuses.Pending)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingApproval != null)
        {
            return await ApproveAsync(pendingApproval.Id, approvedByUserId, notes, scheduledFor, cancellationToken);
        }

        // Fallback: Transition incident directly to Executing
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
        if (incident == null) return false;

        incident.Status = IncidentStatuses.Executing;
        incident.RemediationStartedAt ??= DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;

        var auditEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "remediation_approved",
            AgentName = "ApprovalGate",
            ActionDescription = $"Incident {incidentId} remediation approved by user {approvedByUserId}. Transitioning to {IncidentStatuses.Executing}. Notes: {notes}",
            ExecutionStatus = ExecutionStatuses.Success,
            CreatedByUserId = approvedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_notifier != null)
        {
            try
            {
                await _notifier.NotifyIncidentStatusChangedAsync(
                    incidentId,
                    IncidentStatuses.Executing,
                    DateTimeOffset.UtcNow,
                    cancellationToken).ConfigureAwait(false);
            }
            catch { }
        }

        return true;
    }

    public async Task<bool> DenyByIncidentIdAsync(
        Guid incidentId,
        Guid? deniedByUserId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var pendingApproval = await _dbContext.IncidentEvents
            .Where(e => e.IncidentId == incidentId && e.RequiresApproval && e.ApprovalTimestamp == null && e.ExecutionStatus == ExecutionStatuses.Pending)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (pendingApproval != null)
        {
            return await DenyAsync(pendingApproval.Id, deniedByUserId, reason, cancellationToken);
        }

        // Fallback: Escalate incident directly
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);
        if (incident == null) return false;

        incident.Status = IncidentStatuses.Escalated;
        incident.EscalatedAt ??= DateTime.UtcNow;
        incident.UpdatedAt = DateTime.UtcNow;

        var auditEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = "remediation_denied",
            AgentName = "ApprovalGate",
            ActionDescription = $"Incident {incidentId} remediation denied by user {deniedByUserId}. Reason: {reason}. Incident escalated.",
            ExecutionStatus = ExecutionStatuses.Failed,
            CreatedByUserId = deniedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (_notifier != null)
        {
            try
            {
                await _notifier.NotifyIncidentStatusChangedAsync(
                    incidentId,
                    IncidentStatuses.Escalated,
                    DateTimeOffset.UtcNow,
                    cancellationToken).ConfigureAwait(false);
            }
            catch { }
        }

        return true;
    }
}
