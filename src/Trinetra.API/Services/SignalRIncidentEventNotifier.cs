using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Trinetra.API.Hubs;
using Trinetra.Core.Interfaces;

namespace Trinetra.API.Services;

public class SignalRIncidentEventNotifier : IIncidentEventNotifier
{
    private readonly IHubContext<IncidentHub, IIncidentClient> _hubContext;
    private readonly ILogger<SignalRIncidentEventNotifier> _logger;

    public SignalRIncidentEventNotifier(
        IHubContext<IncidentHub, IIncidentClient> hubContext,
        ILogger<SignalRIncidentEventNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task NotifyIncidentCreatedAsync(Guid incidentId, string title, string severity, string status, DateTime triggeredAt, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                id = incidentId,
                incidentId,
                title,
                severity,
                status,
                triggeredAt
            };
            await _hubContext.Clients.All.IncidentCreated(payload).ConfigureAwait(false);
            _logger.LogDebug("Dispatched IncidentCreated SignalR event for incident {IncidentId}", incidentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast IncidentCreated SignalR event for incident {IncidentId}", incidentId);
        }
    }

    public async Task NotifyIncidentStatusChangedAsync(Guid incidentId, string status, DateTimeOffset updatedAt, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                incidentId,
                status,
                updatedAt
            };
            await _hubContext.Clients.Group($"incident_{incidentId}").IncidentStatusChanged(payload).ConfigureAwait(false);
            await _hubContext.Clients.All.IncidentStatusChanged(payload).ConfigureAwait(false);
            _logger.LogDebug("Dispatched IncidentStatusChanged SignalR event for incident {IncidentId} to {Status}", incidentId, status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast IncidentStatusChanged SignalR event for incident {IncidentId}", incidentId);
        }
    }

    public async Task NotifyAgentActionCompletedAsync(Guid incidentId, string agentName, string actionDescription, string? toolCall, string? toolResult, string status, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                incidentId,
                agentName,
                actionDescription,
                toolCall,
                toolResult,
                status,
                timestamp = DateTimeOffset.UtcNow
            };
            await _hubContext.Clients.Group($"incident_{incidentId}").AgentActionCompleted(payload).ConfigureAwait(false);
            await _hubContext.Clients.All.AgentActionCompleted(payload).ConfigureAwait(false);
            _logger.LogDebug("Dispatched AgentActionCompleted SignalR event for agent {AgentName} on incident {IncidentId}", agentName, incidentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast AgentActionCompleted SignalR event for incident {IncidentId}", incidentId);
        }
    }

    public async Task NotifyApprovalRequiredAsync(Guid incidentId, Guid eventId, string actionDescription, string? toolCall, int? timeoutMinutes, CancellationToken ct = default)
    {
        try
        {
            var payload = new
            {
                incidentId,
                eventId,
                actionDescription,
                toolCall,
                timeoutMinutes,
                timestamp = DateTimeOffset.UtcNow
            };
            await _hubContext.Clients.Group($"incident_{incidentId}").ApprovalRequired(payload).ConfigureAwait(false);
            await _hubContext.Clients.All.ApprovalRequired(payload).ConfigureAwait(false);
            _logger.LogDebug("Dispatched ApprovalRequired SignalR event for event {EventId} on incident {IncidentId}", eventId, incidentId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast ApprovalRequired SignalR event for incident {IncidentId}", incidentId);
        }
    }
}
