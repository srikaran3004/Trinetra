using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Trinetra.Agents.Orchestration;

public class DefaultAgentInvoker : IAgentInvoker
{
    private readonly Dictionary<string, Func<AgentInvocationRequest, CancellationToken, Task<AgentExecutionResult>>> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<DefaultAgentInvoker> _logger;

    public DefaultAgentInvoker(ILogger<DefaultAgentInvoker> logger)
    {
        _logger = logger;
    }

    public void RegisterAgentHandler(string agentName, Func<AgentInvocationRequest, CancellationToken, Task<AgentExecutionResult>> handler)
    {
        _handlers[agentName] = handler;
        _logger.LogInformation("Registered agent handler for '{AgentName}'", agentName);
    }

    public async Task<AgentExecutionResult> InvokeAgentAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        if (_handlers.TryGetValue(request.AgentName, out var handler))
        {
            try
            {
                var result = await handler(request, cancellationToken);
                sw.Stop();
                result.ExecutionTimeMs = sw.ElapsedMilliseconds;
                return result;
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Handler for agent '{AgentName}' threw an exception", request.AgentName);
                return AgentExecutionResult.Failed(request.AgentName, ex.Message, sw.ElapsedMilliseconds);
            }
        }

        sw.Stop();
        _logger.LogWarning("No handler registered for agent '{AgentName}'. Returning simulated response.", request.AgentName);

        // Default mock response when subordinate agents are not yet running
        return AgentExecutionResult.Succeeded(request.AgentName, new
        {
            status = "completed",
            agent = request.AgentName,
            incident_id = request.IncidentId,
            message = $"Subordinate agent '{request.AgentName}' executed action '{request.Action}'"
        }, sw.ElapsedMilliseconds);
    }
}
