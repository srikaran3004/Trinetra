using System;
using System.Threading;
using System.Threading.Tasks;
using Trinetra.Core.Entities;

namespace Trinetra.Core.Interfaces;

public interface IApprovalWorkflowService
{
    Task<IncidentEvent> CreateApprovalRequestAsync(
        Guid incidentId,
        string actionDescription,
        string? toolCall,
        string riskLevel,
        string approvalLevel = "lead",
        int timeoutMinutes = 15,
        CancellationToken cancellationToken = default);

    Task<bool> ApproveAsync(
        Guid approvalId,
        Guid? approvedByUserId,
        string? notes,
        DateTime? scheduledFor = null,
        CancellationToken cancellationToken = default);

    Task<bool> DenyAsync(
        Guid approvalId,
        Guid? deniedByUserId,
        string reason,
        CancellationToken cancellationToken = default);

    Task<bool> ApproveByIncidentIdAsync(
        Guid incidentId,
        Guid? approvedByUserId,
        string? notes,
        DateTime? scheduledFor = null,
        CancellationToken cancellationToken = default);

    Task<bool> DenyByIncidentIdAsync(
        Guid incidentId,
        Guid? deniedByUserId,
        string reason,
        CancellationToken cancellationToken = default);
}
