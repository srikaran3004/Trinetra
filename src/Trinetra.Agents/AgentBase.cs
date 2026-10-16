using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Trinetra.Agents.Configuration;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;

namespace Trinetra.Agents;

/// <summary>
/// Base class for Semantic Kernel agents in Trinetra.
/// Implements tool registration and the non-negotiable audit-event logging ground rule.
/// </summary>
public abstract class AgentBase
{
    public string AgentName { get; }
    public AgentConfiguration Configuration { get; }
    public Kernel Kernel { get; protected set; }
    protected ILogger Logger { get; }
    protected IAuditEventWriter? AuditWriter { get; }

    protected AgentBase(
        string agentName,
        AgentConfiguration configuration,
        Kernel kernel,
        ILogger logger,
        IAuditEventWriter? auditWriter = null)
    {
        AgentName = agentName;
        Configuration = configuration;
        Kernel = kernel;
        Logger = logger;
        AuditWriter = auditWriter;
    }

    /// <summary>
    /// Registers a tool/plugin instance into the agent's Semantic Kernel.
    /// </summary>
    public virtual void RegisterPlugin(object pluginInstance, string? pluginName = null)
    {
        Kernel.Plugins.AddFromObject(pluginInstance, pluginName ?? pluginInstance.GetType().Name);
        Logger.LogInformation("Agent '{AgentName}' registered plugin '{PluginName}'", AgentName, pluginName ?? pluginInstance.GetType().Name);
    }

    /// <summary>
    /// Enforces the non-negotiable ground rule:
    /// "Every agent action writes a row to the incident_events audit table."
    /// </summary>
    public virtual async Task<IncidentEvent?> RecordAuditEventAsync(
        Guid incidentId,
        string eventType,
        string actionDescription,
        string? toolCall = null,
        object? toolParams = null,
        string? toolResult = null,
        string executionStatus = "success",
        string? executionError = null,
        int? executionTimeMs = null,
        bool guardrailTriggered = false,
        string? guardrailName = null,
        string? guardrailReason = null,
        bool requiresApproval = false,
        CancellationToken cancellationToken = default)
    {
        if (AuditWriter == null)
        {
            Logger.LogWarning("No audit writer configured for agent '{AgentName}'. Event '{EventType}' logged only to logger.", AgentName, eventType);
            return null;
        }

        string? serializedParams = null;
        if (toolParams != null)
        {
            serializedParams = toolParams is string str ? str : JsonSerializer.Serialize(toolParams);
        }

        var incidentEvent = new IncidentEvent
        {
            IncidentId = incidentId,
            EventType = eventType,
            AgentName = AgentName,
            ActionDescription = actionDescription,
            ToolCall = toolCall,
            ToolParams = serializedParams,
            ToolResult = toolResult,
            ExecutionStatus = executionStatus,
            ExecutionError = executionError,
            ExecutionTimeMs = executionTimeMs,
            GuardrailTriggered = guardrailTriggered,
            GuardrailName = guardrailName,
            GuardrailReason = guardrailReason,
            RequiresApproval = requiresApproval,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            var savedEvent = await AuditWriter.WriteEventAsync(incidentEvent, cancellationToken);
            Logger.LogInformation("Agent '{AgentName}' recorded audit event '{EventType}' for incident {IncidentId}", AgentName, eventType, incidentId);
            return savedEvent;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to write audit event for agent '{AgentName}', incident {IncidentId}", AgentName, incidentId);
            throw;
        }
    }
}
