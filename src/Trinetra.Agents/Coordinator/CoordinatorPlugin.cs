using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Agents.Orchestration;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents.Coordinator;

public class CoordinatorPlugin
{
    private readonly IIncidentCoordinatorStore _store;
    private readonly IAgentInvoker _agentInvoker;
    private readonly IAuditEventWriter? _auditWriter;
    private readonly ILogger<CoordinatorPlugin> _logger;

    public CoordinatorPlugin(
        IIncidentCoordinatorStore store,
        IAgentInvoker agentInvoker,
        IAuditEventWriter? auditWriter,
        ILogger<CoordinatorPlugin> logger)
    {
        _store = store;
        _agentInvoker = agentInvoker;
        _auditWriter = auditWriter;
        _logger = logger;
    }

    [KernelFunction("create_incident")]
    [Description("Creates an incident record in the database, sets initial status to TRIGGERED, and logs an audit event.")]
    public async Task<string> CreateIncidentAsync(
        [Description("Incident title")] string title,
        [Description("Source of the alert, e.g. prometheus, datadog")] string source,
        [Description("Severity: critical, high, medium, low")] string severity,
        [Description("Detailed incident description")] string description,
        [Description("JSON array of affected service names, e.g. [\"auth-api\"]")] string affectedServicesJson,
        CancellationToken cancellationToken = default)
    {
        var services = new List<string>();
        if (!string.IsNullOrWhiteSpace(affectedServicesJson))
        {
            try
            {
                services = JsonSerializer.Deserialize<List<string>>(affectedServicesJson) ?? new List<string>();
            }
            catch
            {
                services.Add(affectedServicesJson);
            }
        }

        var incident = new Incident
        {
            AlertId = $"alert-{Guid.NewGuid():N}",
            Title = title,
            Source = source,
            Severity = string.IsNullOrWhiteSpace(severity) ? IncidentSeverities.Medium : severity.ToLower(),
            Description = description,
            Status = IncidentStatuses.Triggered,
            AffectedServices = services,
            TriggeredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _store.CreateIncidentAsync(incident, cancellationToken);

        if (_auditWriter != null)
        {
            await _auditWriter.WriteEventAsync(new IncidentEvent
            {
                IncidentId = created.Id,
                EventType = "incident_created",
                AgentName = "IncidentCoordinator",
                ActionDescription = $"Incident created with status '{created.Status}' and severity '{created.Severity}'",
                ToolCall = "create_incident",
                ToolParams = JsonSerializer.Serialize(new { title, source, severity }),
                ExecutionStatus = ExecutionStatuses.Success,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return JsonSerializer.Serialize(new
        {
            incident_id = created.Id,
            status = created.Status,
            alert_id = created.AlertId
        });
    }

    [KernelFunction("update_incident_status")]
    [Description("Updates the status of an incident according to the state machine, records state_changed audit event.")]
    public async Task<string> UpdateIncidentStatusAsync(
        [Description("The unique UUID of the incident")] string incidentId,
        [Description("Target status: investigating, analysis_complete, awaiting_approval, executing, monitoring, resolved, escalated")] string newStatus,
        [Description("Reason for the state change")] string reason,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(incidentId, out var id))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Invalid incident UUID format" });
        }

        var current = await _store.GetIncidentAsync(id, cancellationToken);
        if (current == null)
        {
            return JsonSerializer.Serialize(new { success = false, error = "Incident not found" });
        }

        // Validate state machine transition
        try
        {
            IncidentStateMachine.ValidateTransition(current.Status, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "State transition validation failed for incident {IncidentId}", id);
            return JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }

        var oldStatus = current.Status;
        var updated = await _store.UpdateIncidentStatusAsync(id, newStatus, reason, cancellationToken);

        if (_auditWriter != null)
        {
            await _auditWriter.WriteEventAsync(new IncidentEvent
            {
                IncidentId = id,
                EventType = "state_changed",
                AgentName = "IncidentCoordinator",
                ActionDescription = $"State transition from '{oldStatus}' to '{newStatus}'. Reason: {reason}",
                ToolCall = "update_incident_status",
                ToolParams = JsonSerializer.Serialize(new { oldStatus, newStatus, reason }),
                ExecutionStatus = ExecutionStatuses.Success,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return JsonSerializer.Serialize(new { success = updated, incident_id = id, from_state = oldStatus, to_state = newStatus });
    }

    [KernelFunction("invoke_agent")]
    [Description("Invokes a specific subordinate agent (e.g. log_analyzer, metrics_querier, runbook_retriever, root_cause_analyzer, remediation_proposer, auto_executor, approval_gate).")]
    public async Task<string> InvokeAgentAsync(
        [Description("Target agent name")] string agentName,
        [Description("Incident UUID")] string incidentId,
        [Description("Action or task description to execute")] string action,
        [Description("JSON string of parameters for the agent")] string parametersJson,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(incidentId, out var id))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Invalid incident UUID" });
        }

        var parameters = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(parametersJson))
        {
            try
            {
                parameters = JsonSerializer.Deserialize<Dictionary<string, object?>>(parametersJson) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse parameters JSON for agent {AgentName}", agentName);
            }
        }

        var request = new AgentInvocationRequest
        {
            IncidentId = id,
            AgentName = agentName,
            Action = action,
            Parameters = parameters
        };

        if (_auditWriter != null)
        {
            await _auditWriter.WriteEventAsync(new IncidentEvent
            {
                IncidentId = id,
                EventType = "agent_invoked",
                AgentName = "IncidentCoordinator",
                ActionDescription = $"Invoking agent '{agentName}' with action '{action}'",
                ToolCall = "invoke_agent",
                ToolParams = JsonSerializer.Serialize(new { agentName, action }),
                ExecutionStatus = ExecutionStatuses.InProgress,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        var result = await _agentInvoker.InvokeAgentAsync(request, cancellationToken);

        if (_auditWriter != null)
        {
            await _auditWriter.WriteEventAsync(new IncidentEvent
            {
                IncidentId = id,
                EventType = "agent_completed",
                AgentName = agentName,
                ActionDescription = $"Agent '{agentName}' completed action '{action}' (Success={result.Success})",
                ToolCall = "invoke_agent",
                ToolResult = result.OutputJson ?? JsonSerializer.Serialize(result.OutputData),
                ExecutionStatus = result.Success ? ExecutionStatuses.Success : ExecutionStatuses.Failed,
                ExecutionError = result.ErrorMessage,
                ExecutionTimeMs = (int)result.ExecutionTimeMs,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return JsonSerializer.Serialize(result);
    }

    [KernelFunction("wait_for_agents")]
    [Description("Waits for multiple agents in parallel with a timeout. Returns a consolidated result map.")]
    public async Task<string> WaitForAgentsAsync(
        [Description("Comma-separated list of agent names, e.g. 'log_analyzer,metrics_querier,runbook_retriever'")] string agentNamesCommaSeparated,
        [Description("Incident UUID")] string incidentId,
        [Description("Timeout in seconds")] int timeoutSeconds = 300,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(incidentId, out var id))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Invalid incident UUID" });
        }

        var agents = agentNamesCommaSeparated
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        var tasks = agents.Select(async agent =>
        {
            try
            {
                var req = new AgentInvocationRequest
                {
                    IncidentId = id,
                    AgentName = agent,
                    Action = "investigate",
                    TimeoutSeconds = timeoutSeconds
                };
                return await _agentInvoker.InvokeAgentAsync(req, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return AgentExecutionResult.Failed(agent, $"Agent '{agent}' timed out after {timeoutSeconds}s.");
            }
            catch (Exception ex)
            {
                return AgentExecutionResult.Failed(agent, ex.Message);
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);
        var resultMap = results.ToDictionary(r => r.AgentName, r => r);

        return JsonSerializer.Serialize(resultMap);
    }

    [KernelFunction("escalate_incident")]
    [Description("Marks an incident as ESCALATED with a reason, records audit event, and halts autonomous execution.")]
    public async Task<string> EscalateIncidentAsync(
        [Description("Incident UUID")] string incidentId,
        [Description("Reason for escalation")] string reason,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(incidentId, out var id))
        {
            return JsonSerializer.Serialize(new { success = false, error = "Invalid incident UUID" });
        }

        var success = await _store.EscalateIncidentAsync(id, reason, cancellationToken);

        if (_auditWriter != null)
        {
            await _auditWriter.WriteEventAsync(new IncidentEvent
            {
                IncidentId = id,
                EventType = "incident_escalated",
                AgentName = "IncidentCoordinator",
                ActionDescription = $"Incident escalated: {reason}",
                ToolCall = "escalate_incident",
                ToolParams = JsonSerializer.Serialize(new { reason }),
                ExecutionStatus = ExecutionStatuses.Success,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return JsonSerializer.Serialize(new { success, incident_id = id, status = IncidentStatuses.Escalated, reason });
    }
}
