using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trinetra.API.DTOs;
using Trinetra.Core.Entities;
using Trinetra.Infrastructure.Data;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RunbooksController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public RunbooksController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates a new operational runbook. Embedding is generated asynchronously post-insert.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRunbook([FromBody] CreateRunbookRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "BAD_REQUEST",
                    Message = "Title and Content are required",
                    Status = StatusCodes.Status400BadRequest
                }
            });
        }

        var runbook = new Runbook
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Tags = request.Tags ?? new List<string>(),
            ApplicableServices = request.ApplicableServices ?? new List<string>(),
            Content = request.Content,
            RemediationSteps = request.RemediationSteps != null ? JsonSerializer.Serialize(request.RemediationSteps) : null,
            EstimatedTimeMinutes = request.EstimatedTimeMinutes,
            ContentEmbedding = null, // Nullable per Decision D-11 (generated async)
            IsActive = true,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Runbooks.Add(runbook);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRunbookById), new { id = runbook.Id }, new { id = runbook.Id, created_at = runbook.CreatedAt });
    }

    /// <summary>
    /// Lists runbooks with search and tag filtering.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RunbookListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRunbooks(
        [FromQuery] string? tags,
        [FromQuery] string? services,
        [FromQuery] string? search,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        limit = limit <= 0 ? 20 : Math.Min(limit, 100);
        offset = Math.Max(0, offset);

        var query = _dbContext.Runbooks.Where(r => r.IsActive).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(r => r.Title.ToLower().Contains(searchLower) || (r.Description != null && r.Description.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(r => new RunbookListItemDto
            {
                Id = r.Id,
                Title = r.Title,
                Category = r.Category,
                Tags = r.Tags,
                ApplicableServices = r.ApplicableServices,
                EstimatedTimeMinutes = r.EstimatedTimeMinutes,
                IsActive = r.IsActive,
                Version = r.Version
            })
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<RunbookListItemDto>
        {
            Items = items,
            Total = total,
            Limit = limit,
            Offset = offset
        });
    }

    /// <summary>
    /// Gets runbook details by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RunbookDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRunbookById(Guid id, CancellationToken cancellationToken)
    {
        var runbook = await _dbContext.Runbooks.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (runbook == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "NOT_FOUND",
                    Message = $"Runbook with ID '{id}' was not found",
                    Status = StatusCodes.Status404NotFound
                }
            });
        }

        return Ok(new RunbookDetailDto
        {
            Id = runbook.Id,
            Title = runbook.Title,
            Description = runbook.Description,
            Category = runbook.Category,
            Tags = runbook.Tags,
            ApplicableServices = runbook.ApplicableServices,
            Content = runbook.Content,
            RemediationSteps = runbook.RemediationSteps,
            EstimatedTimeMinutes = runbook.EstimatedTimeMinutes,
            Version = runbook.Version,
            IsActive = runbook.IsActive,
            CreatedAt = runbook.CreatedAt,
            UpdatedAt = runbook.UpdatedAt
        });
    }
}
