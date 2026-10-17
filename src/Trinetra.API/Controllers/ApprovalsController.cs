using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trinetra.API.DTOs;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Data;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ApprovalsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IApprovalWorkflowService _workflowService;

    public ApprovalsController(
        ApplicationDbContext dbContext,
        IApprovalWorkflowService workflowService)
    {
        _dbContext = dbContext;
        _workflowService = workflowService;
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
        Guid? approverId = request.ApprovedByUserId;
        if (!approverId.HasValue)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var parsed)) approverId = parsed;
        }

        var success = await _workflowService.ApproveAsync(
            id,
            approverId,
            request.Notes,
            request.ScheduledFor,
            cancellationToken);

        if (!success)
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

        return Ok(new ApproveResponse
        {
            ApprovalId = id,
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
        Guid? denierId = request.DeniedByUserId;
        if (!denierId.HasValue)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(sub, out var parsed)) denierId = parsed;
        }

        var success = await _workflowService.DenyAsync(
            id,
            denierId,
            request.Reason ?? "Denied by operator",
            cancellationToken);

        if (!success)
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

        return Ok(new DenyResponse
        {
            ApprovalId = id,
            Status = "denied",
            DeniedAt = DateTime.UtcNow,
            IncidentStatus = IncidentStatuses.Escalated
        });
    }
}
