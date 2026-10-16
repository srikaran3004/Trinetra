using Microsoft.Extensions.Logging;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.Infrastructure.Services;

public class DbAuditEventWriter : IAuditEventWriter
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DbAuditEventWriter> _logger;
    private readonly IIncidentEventNotifier? _notifier;

    public DbAuditEventWriter(
        ApplicationDbContext dbContext,
        ILogger<DbAuditEventWriter> logger,
        IIncidentEventNotifier? notifier = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notifier = notifier;
    }

    public async Task<IncidentEvent> WriteEventAsync(IncidentEvent incidentEvent, CancellationToken cancellationToken = default)
    {
        _dbContext.IncidentEvents.Add(incidentEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Audit event written to DB: {EventType} by {AgentName} for incident {IncidentId}",
            incidentEvent.EventType, incidentEvent.AgentName, incidentEvent.IncidentId);

        if (_notifier != null)
        {
            try
            {
                if (incidentEvent.RequiresApproval || incidentEvent.EventType == "approval_required")
                {
                    await _notifier.NotifyApprovalRequiredAsync(
                        incidentEvent.IncidentId,
                        incidentEvent.Id,
                        incidentEvent.ActionDescription ?? string.Empty,
                        incidentEvent.ToolCall,
                        incidentEvent.ApprovalTimeoutMinutes,
                        cancellationToken).ConfigureAwait(false);
                }
                else if (!string.IsNullOrEmpty(incidentEvent.AgentName) &&
                         (incidentEvent.EventType.EndsWith("_completed", StringComparison.OrdinalIgnoreCase) ||
                          incidentEvent.EventType.Equals("agent_action_completed", StringComparison.OrdinalIgnoreCase)))
                {
                    await _notifier.NotifyAgentActionCompletedAsync(
                        incidentEvent.IncidentId,
                        incidentEvent.AgentName,
                        incidentEvent.ActionDescription ?? string.Empty,
                        incidentEvent.ToolCall,
                        incidentEvent.ToolResult,
                        incidentEvent.ExecutionStatus,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch real-time event notification for {EventType}", incidentEvent.EventType);
            }
        }

        return incidentEvent;
    }
}
