using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Trinetra.API.DTOs;
using Trinetra.API.Services;

namespace Trinetra.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IIncidentService incidentService, ILogger<WebhooksController> logger)
    {
        _incidentService = incidentService;
        _logger = logger;
    }

    /// <summary>
    /// Ingests alert payloads in Prometheus or Datadog format and triggers an incident.
    /// </summary>
    [HttpPost("alerts")]
    [ProducesResponseType(typeof(WebhookAlertResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestAlert([FromBody] JsonElement rawPayload, CancellationToken cancellationToken)
    {
        try
        {
            // Detect format: Prometheus has "alerts" array, Datadog has "alert" object
            if (rawPayload.TryGetProperty("alerts", out _))
            {
                var promPayload = JsonSerializer.Deserialize<PrometheusAlertPayload>(rawPayload.GetRawText());
                if (promPayload != null)
                {
                    var response = await _incidentService.ProcessPrometheusAlertAsync(promPayload, cancellationToken);
                    return Ok(response);
                }
            }
            else if (rawPayload.TryGetProperty("alert", out _))
            {
                var ddPayload = JsonSerializer.Deserialize<DatadogAlertPayload>(rawPayload.GetRawText());
                if (ddPayload != null)
                {
                    var response = await _incidentService.ProcessDatadogAlertAsync(ddPayload, cancellationToken);
                    return Ok(response);
                }
            }

            // Fallback: try parsing generic incident
            var generic = JsonSerializer.Deserialize<CreateIncidentRequest>(rawPayload.GetRawText());
            if (generic != null && !string.IsNullOrWhiteSpace(generic.Title))
            {
                var created = await _incidentService.CreateIncidentAsync(generic, cancellationToken: cancellationToken);
                return Ok(new WebhookAlertResponse
                {
                    IncidentId = created.IncidentId,
                    Status = created.Status,
                    InvestigationStarting = true
                });
            }

            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "BAD_REQUEST",
                    Message = "Unsupported alert format. Expected Prometheus or Datadog alert payload.",
                    Status = StatusCodes.Status400BadRequest
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse webhook alert payload");
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = "PARSE_ERROR",
                    Message = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                }
            });
        }
    }
}
