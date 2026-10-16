using System;
using System.Threading;
using System.Threading.Tasks;

namespace Trinetra.Core.Interfaces;

public interface IIncidentEventNotifier
{
    Task NotifyIncidentCreatedAsync(Guid incidentId, string title, string severity, string status, DateTime triggeredAt, CancellationToken ct = default);
    Task NotifyIncidentStatusChangedAsync(Guid incidentId, string status, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task NotifyAgentActionCompletedAsync(Guid incidentId, string agentName, string actionDescription, string? toolCall, string? toolResult, string status, CancellationToken ct = default);
    Task NotifyApprovalRequiredAsync(Guid incidentId, Guid eventId, string actionDescription, string? toolCall, int? timeoutMinutes, CancellationToken ct = default);
}
