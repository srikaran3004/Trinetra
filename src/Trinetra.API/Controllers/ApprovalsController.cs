using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trinetra.API.DTOs;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Infrastructure.Data;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public ApprovalsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Lists all pending approval requests.
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(PagedResult<PendingApprovalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingApprovals(
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        limit = limit <= 0 ? 20 : Math.Min(limit, 100);
        offset = Math.Max(0, offset);

        var query = _dbContext.IncidentEvents
            .Where(e => e.RequiresApproval && e.ApprovalTimestamp == null && e.ExecutionStatus == ExecutionStatuses.Pending)
            .AsNoTracking();

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(e => new PendingApprovalDto
            {
                Id = e.Id,
                IncidentId = e.IncidentId,
                Action = e.ActionDescription ?? "Remediation action requiring approval",
                RiskLevel = "medium",
                ApprovalLevel = "lead",
                TimeoutAt = e.ApprovalTimeoutMinutes.HasValue ? e.CreatedAt.AddMinutes(e.ApprovalTimeoutMinutes.Value) : null,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<PendingApprovalDto>
        {
            Items = items,
            Total = total,
            Limit = limit,
            Offset = offset
        });
    }

    /// <summary>
    /// Approves a remediation action.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "admin,lead,manager,sre")]
    [ProducesResponseType(typeof(ApproveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] ApproveRequest request,
        CancellationToken cancellationToken)
    {
        var evt = await _dbContext.IncidentEvents
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evt == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "NOT_FOUND",
                    Message = $"Approval request with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                }
            });
        }

        Guid? approverId = request.ApprovedByUserId;
        if (!approverId.HasValue)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var parsed)) approverId = parsed;
        }

        evt.ApprovedBy = approverId;
        evt.ApprovalTimestamp = DateTime.UtcNow;
        evt.ApprovalNotes = request.Notes;
        evt.ExecutionStatus = ExecutionStatuses.Success;

        // Ground rule: Audit event for approval write
        var auditEvent = new IncidentEvent
        {
            IncidentId = evt.IncidentId,
            EventType = "remediation_approved",
            AgentName = "ApprovalGate",
            ActionDescription = $"Action '{evt.ActionDescription}' approved by user {approverId}. Notes: {request.Notes}",
            ExecutionStatus = ExecutionStatuses.Success,
            CreatedByUserId = approverId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ApproveResponse
        {
            ApprovalId = evt.Id,
            Status = "approved",
            ApprovedAt = DateTime.UtcNow,
            RemediationWillExecuteAt = request.ScheduledFor ?? DateTime.UtcNow
        });
    }

    /// <summary>
    /// Denies a remediation action and escalates the incident.
    /// </summary>
    [HttpPost("{id:guid}/deny")]
    [Authorize(Roles = "admin,lead,manager,sre")]
    [ProducesResponseType(typeof(DenyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deny(
        Guid id,
        [FromBody] DenyRequest request,
        CancellationToken cancellationToken)
    {
        var evt = await _dbContext.IncidentEvents
            .Include(e => e.Incident)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (evt == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "NOT_FOUND",
                    Message = $"Approval request with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                }
            });
        }

        Guid? denierId = request.DeniedByUserId;
        if (!denierId.HasValue)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var parsed)) denierId = parsed;
        }

        evt.ApprovalTimestamp = DateTime.UtcNow;
        evt.ApprovalNotes = $"Denied: {request.Reason}";
        evt.ExecutionStatus = ExecutionStatuses.Failed;

        // Escalate incident
        if (evt.Incident != null)
        {
            evt.Incident.Status = IncidentStatuses.Escalated;
            evt.Incident.EscalatedAt = DateTime.UtcNow;
            evt.Incident.UpdatedAt = DateTime.UtcNow;
        }

        // Ground rule: Audit event for denial write
        var auditEvent = new IncidentEvent
        {
            IncidentId = evt.IncidentId,
            EventType = "remediation_denied",
            AgentName = "ApprovalGate",
            ActionDescription = $"Action '{evt.ActionDescription}' denied by user {denierId}. Reason: {request.Reason}. Incident escalated.",
            ExecutionStatus = ExecutionStatuses.Failed,
            CreatedByUserId = denierId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(auditEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new DenyResponse
        {
            ApprovalId = evt.Id,
            Status = "denied",
            DeniedAt = DateTime.UtcNow,
            IncidentStatus = IncidentStatuses.Escalated
        });
    }
}
