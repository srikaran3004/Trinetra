using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Trinetra.API.DTOs;
using Trinetra.API.Hubs;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Infrastructure.Data;

namespace Trinetra.API.Services;

public interface IIncidentService
{
    Task<CreateIncidentResponse> CreateIncidentAsync(CreateIncidentRequest request, Guid? createdByUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<IncidentListItemDto>> GetIncidentsAsync(string? status, string? severity, int limit, int offset, string? sortBy, CancellationToken cancellationToken = default);
    Task<IncidentDetailDto?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UpdateIncidentAsync(Guid id, UpdateIncidentRequest request, Guid? updatedByUserId = null, CancellationToken cancellationToken = default);
    Task<PagedResult<IncidentEventDto>> GetIncidentEventsAsync(Guid incidentId, string? eventType, int limit, int offset, CancellationToken cancellationToken = default);
    Task<WebhookAlertResponse> ProcessPrometheusAlertAsync(PrometheusAlertPayload payload, CancellationToken cancellationToken = default);
    Task<WebhookAlertResponse> ProcessDatadogAlertAsync(DatadogAlertPayload payload, CancellationToken cancellationToken = default);
}

public class IncidentService : IIncidentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IHubContext<IncidentHub, IIncidentClient> _hubContext;
    private readonly ILogger<IncidentService> _logger;

    public IncidentService(
        ApplicationDbContext dbContext,
        IHubContext<IncidentHub, IIncidentClient> hubContext,
        ILogger<IncidentService> logger)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<CreateIncidentResponse> CreateIncidentAsync(
        CreateIncidentRequest request,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var services = request.AffectedServices ?? new List<string>();
        if (!string.IsNullOrWhiteSpace(request.AffectedService) && !services.Contains(request.AffectedService))
        {
            services.Add(request.AffectedService);
        }

        var incident = new Incident
        {
            AlertId = string.IsNullOrWhiteSpace(request.AlertId) ? $"alert-{Guid.NewGuid():N}" : request.AlertId,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source,
            Title = request.Title,
            Description = request.Description,
            Severity = string.IsNullOrWhiteSpace(request.Severity) ? IncidentSeverities.Medium : request.Severity.ToLower(),
            Status = IncidentStatuses.Triggered,
            AffectedServices = services,
            AffectedHosts = request.AffectedHosts ?? new List<string>(),
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Incidents.Add(incident);

        // Ground Rule: EVERY write to incidents DB must also write a row to incident_events
        var createdEvent = new IncidentEvent
        {
            IncidentId = incident.Id,
            EventType = "incident_created",
            AgentName = "System",
            ActionDescription = $"Incident created from source '{incident.Source}' with severity '{incident.Severity}'",
            ToolParams = request.AlertData != null ? JsonSerializer.Serialize(request.AlertData) : null,
            ExecutionStatus = ExecutionStatuses.Success,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(createdEvent);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created incident {IncidentId} with alert {AlertId}", incident.Id, incident.AlertId);

        // SignalR notification
        try
        {
            await _hubContext.Clients.All.IncidentCreated(new
            {
                id = incident.Id,
                title = incident.Title,
                severity = incident.Severity,
                status = incident.Status,
                triggeredAt = incident.TriggeredAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast SignalR event for incident {IncidentId}", incident.Id);
        }

        return new CreateIncidentResponse
        {
            IncidentId = incident.Id,
            AlertId = incident.AlertId,
            Status = incident.Status,
            Severity = incident.Severity,
            CreatedAt = incident.CreatedAt
        };
    }

    public async Task<PagedResult<IncidentListItemDto>> GetIncidentsAsync(
        string? status,
        string? severity,
        int limit,
        int offset,
        string? sortBy,
        CancellationToken cancellationToken = default)
    {
        limit = limit <= 0 ? 50 : Math.Min(limit, 100);
        offset = Math.Max(0, offset);

        var query = _dbContext.Incidents
            .Include(i => i.AssignedUser)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLower())
                .ToList();
            query = query.Where(i => statuses.Contains(i.Status.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            var severities = severity.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => s.ToLower())
                .ToList();
            query = query.Where(i => severities.Contains(i.Severity.ToLower()));
        }

        var total = await query.CountAsync(cancellationToken);

        query = sortBy?.ToLower() switch
        {
            "triggered_at_asc" or "asc" => query.OrderBy(i => i.TriggeredAt),
            _ => query.OrderByDescending(i => i.TriggeredAt)
        };

        var items = await query
            .Skip(offset)
            .Take(limit)
            .Select(i => new IncidentListItemDto
            {
                Id = i.Id,
                AlertId = i.AlertId,
                Title = i.Title,
                Severity = i.Severity,
                Status = i.Status,
                TriggeredAt = i.TriggeredAt,
                RootCause = i.RootCause,
                AssignedTo = i.AssignedTo,
                AssignedUserName = i.AssignedUser != null ? i.AssignedUser.Username : null
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<IncidentListItemDto>
        {
            Items = items,
            Total = total,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<IncidentDetailDto?> GetIncidentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents
            .Include(i => i.Events.OrderBy(e => e.CreatedAt))
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (incident == null)
            return null;

        return new IncidentDetailDto
        {
            Id = incident.Id,
            AlertId = incident.AlertId,
            Source = incident.Source,
            Title = incident.Title,
            Description = incident.Description,
            Severity = incident.Severity,
            Status = incident.Status,
            AffectedServices = incident.AffectedServices,
            AffectedHosts = incident.AffectedHosts,
            RootCause = incident.RootCause,
            RootCauseConfidence = incident.RootCauseConfidence,
            ProposedRemediation = incident.ProposedRemediation,
            RemediationRiskLevel = incident.RemediationRiskLevel,
            TriggeredAt = incident.TriggeredAt,
            StartedInvestigatingAt = incident.StartedInvestigatingAt,
            ResolvedAt = incident.ResolvedAt,
            AssignedTo = incident.AssignedTo,
            Events = incident.Events.Select(e => new IncidentEventDto
            {
                Id = e.Id,
                IncidentId = e.IncidentId,
                EventType = e.EventType,
                AgentName = e.AgentName,
                ActionDescription = e.ActionDescription,
                ToolCall = e.ToolCall,
                ToolParams = e.ToolParams,
                ToolResult = e.ToolResult,
                ExecutionStatus = e.ExecutionStatus,
                GuardrailTriggered = e.GuardrailTriggered,
                RequiresApproval = e.RequiresApproval,
                Timestamp = e.CreatedAt
            })
        };
    }

    public async Task<bool> UpdateIncidentAsync(
        Guid id,
        UpdateIncidentRequest request,
        Guid? updatedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (incident == null)
            return false;

        var changes = new List<string>();

        if (request.AssignedTo.HasValue && request.AssignedTo != incident.AssignedTo)
        {
            incident.AssignedTo = request.AssignedTo;
            changes.Add($"Assigned to {request.AssignedTo}");
        }

        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status != incident.Status)
        {
            var oldStatus = incident.Status;
            incident.Status = request.Status;
            changes.Add($"Status changed from {oldStatus} to {incident.Status}");

            if (incident.Status == IncidentStatuses.Investigating && incident.StartedInvestigatingAt == null)
            {
                incident.StartedInvestigatingAt = DateTime.UtcNow;
            }
            else if (incident.Status == IncidentStatuses.Resolved && incident.ResolvedAt == null)
            {
                incident.ResolvedAt = DateTime.UtcNow;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            changes.Add($"Notes: {request.Notes}");
        }

        incident.UpdatedAt = DateTime.UtcNow;

        // Ground Rule: EVERY write to incidents DB must also write a row to incident_events
        var updateEvent = new IncidentEvent
        {
            IncidentId = incident.Id,
            EventType = "incident_updated",
            AgentName = "User",
            ActionDescription = changes.Count > 0 ? string.Join("; ", changes) : "Incident updated",
            ExecutionStatus = ExecutionStatuses.Success,
            CreatedByUserId = updatedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.IncidentEvents.Add(updateEvent);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast status change
        try
        {
            await _hubContext.Clients.All.IncidentStatusChanged(new
            {
                incidentId = incident.Id,
                status = incident.Status,
                updatedAt = incident.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast status change for incident {IncidentId}", incident.Id);
        }

        return true;
    }

    public async Task<PagedResult<IncidentEventDto>> GetIncidentEventsAsync(
        Guid incidentId,
        string? eventType,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        limit = limit <= 0 ? 100 : Math.Min(limit, 200);
        offset = Math.Max(0, offset);

        var query = _dbContext.IncidentEvents
            .Where(e => e.IncidentId == incidentId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query = query.Where(e => e.EventType.ToLower() == eventType.ToLower());
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .Select(e => new IncidentEventDto
            {
                Id = e.Id,
                IncidentId = e.IncidentId,
                EventType = e.EventType,
                AgentName = e.AgentName,
                ActionDescription = e.ActionDescription,
                ToolCall = e.ToolCall,
                ToolParams = e.ToolParams,
                ToolResult = e.ToolResult,
                ExecutionStatus = e.ExecutionStatus,
                GuardrailTriggered = e.GuardrailTriggered,
                RequiresApproval = e.RequiresApproval,
                Timestamp = e.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<IncidentEventDto>
        {
            Items = items,
            Total = total,
            Limit = limit,
            Offset = offset
        };
    }

    public async Task<WebhookAlertResponse> ProcessPrometheusAlertAsync(
        PrometheusAlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        var firstAlert = payload.Alerts?.FirstOrDefault();
        var alertName = firstAlert?.Labels?.GetValueOrDefault("alertname") ?? "Prometheus Alert";
        var service = firstAlert?.Labels?.GetValueOrDefault("service") ?? "unknown-service";
        var severity = firstAlert?.Labels?.GetValueOrDefault("severity") ?? IncidentSeverities.High;
        var description = firstAlert?.Annotations?.GetValueOrDefault("description") ?? firstAlert?.Annotations?.GetValueOrDefault("summary");

        var createReq = new CreateIncidentRequest
        {
            AlertId = $"prom-{Guid.NewGuid():N}",
            Source = "prometheus",
            Title = $"{alertName} in {service}",
            Description = description,
            Severity = severity,
            AffectedService = service,
            AlertData = payload
        };

        var response = await CreateIncidentAsync(createReq, cancellationToken: cancellationToken);

        return new WebhookAlertResponse
        {
            IncidentId = response.IncidentId,
            Status = response.Status,
            InvestigationStarting = true
        };
    }

    public async Task<WebhookAlertResponse> ProcessDatadogAlertAsync(
        DatadogAlertPayload payload,
        CancellationToken cancellationToken = default)
    {
        var alert = payload.Alert;
        var alertId = alert?.Id?.ToString() ?? Guid.NewGuid().ToString("N");
        var title = alert?.Title ?? "Datadog Alert";
        var metric = alert?.Metric ?? "datadog.metric";

        var createReq = new CreateIncidentRequest
        {
            AlertId = $"dd-{alertId}",
            Source = "datadog",
            Title = title,
            Description = $"Alert on metric: {metric}. Current value: {alert?.CurrentValue}, Threshold: {alert?.ThresholdValue}",
            Severity = IncidentSeverities.High,
            AlertData = payload
        };

        var response = await CreateIncidentAsync(createReq, cancellationToken: cancellationToken);

        return new WebhookAlertResponse
        {
            IncidentId = response.IncidentId,
            Status = response.Status,
            InvestigationStarting = true
        };
    }
}
