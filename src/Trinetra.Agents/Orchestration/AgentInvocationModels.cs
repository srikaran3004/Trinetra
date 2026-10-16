namespace Trinetra.Agents.Orchestration;

public class AgentInvocationRequest
{
    public Guid IncidentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new();
    public int TimeoutSeconds { get; set; } = 300;
}

public class AgentExecutionResult
{
    public string AgentName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? OutputJson { get; set; }
    public object? OutputData { get; set; }
    public string? OutputSummary { get; set; }
    public string? ErrorMessage { get; set; }
    public long ExecutionTimeMs { get; set; }

    public static AgentExecutionResult Succeeded(string agentName, object? data, long elapsedMs = 0) => new()
    {
        AgentName = agentName,
        Success = true,
        OutputData = data,
        ExecutionTimeMs = elapsedMs
    };

    public static AgentExecutionResult Failed(string agentName, string error, long elapsedMs = 0) => new()
    {
        AgentName = agentName,
        Success = false,
        ErrorMessage = error,
        ExecutionTimeMs = elapsedMs
    };
}

public interface IAgentInvoker
{
    Task<AgentExecutionResult> InvokeAgentAsync(AgentInvocationRequest request, CancellationToken cancellationToken = default);
}
