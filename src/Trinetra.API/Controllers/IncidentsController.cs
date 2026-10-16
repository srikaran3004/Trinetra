using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trinetra.API.DTOs;
using Trinetra.API.Services;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    /// <summary>
    /// Creates a new incident.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateIncidentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIncident([FromBody] CreateIncidentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "BAD_REQUEST",
                    Message = "Title is required",
                    Status = StatusCodes.Status400BadRequest
                }
            });
        }

        Guid? userId = null;
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(subClaim, out var parsedGuid))
        {
            userId = parsedGuid;
        }

        var response = await _incidentService.CreateIncidentAsync(request, userId, cancellationToken);
        return CreatedAtAction(nameof(GetIncidentById), new { id = response.IncidentId }, response);
    }

    /// <summary>
    /// Lists incidents with filtering and pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<IncidentListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidents(
        [FromQuery] string? status,
        [FromQuery] string? severity,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? sort_by = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _incidentService.GetIncidentsAsync(status, severity, limit, offset, sort_by, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Gets detailed information for a specific incident, including audit events.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(IncidentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncidentById(Guid id, CancellationToken cancellationToken)
    {
        var incident = await _incidentService.GetIncidentByIdAsync(id, cancellationToken);
        if (incident == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INCIDENT_NOT_FOUND",
                    Message = $"Incident with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                }
            });
        }

        return Ok(incident);
    }

    /// <summary>
    /// Updates incident assignment, status, or notes.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIncident(
        Guid id,
        [FromBody] UpdateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        Guid? userId = null;
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(subClaim, out var parsedGuid))
        {
            userId = parsedGuid;
        }

        var updated = await _incidentService.UpdateIncidentAsync(id, request, userId, cancellationToken);
        if (!updated)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "INCIDENT_NOT_FOUND",
                    Message = $"Incident with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                }
            });
        }

        return Ok(new { id, updated_at = DateTime.UtcNow });
    }

    /// <summary>
    /// Gets paginated audit trail events for an incident.
    /// </summary>
    [HttpGet("{id:guid}/events")]
    [ProducesResponseType(typeof(PagedResult<IncidentEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidentEvents(
        Guid id,
        [FromQuery] string? event_type,
        [FromQuery] int limit = 100,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var result = await _incidentService.GetIncidentEventsAsync(id, event_type, limit, offset, cancellationToken);
        return Ok(result);
    }
}
