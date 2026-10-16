namespace Trinetra.Agents;

/// <summary>
/// Base class for Semantic Kernel agents in Trinetra.
/// Will be fully implemented in Phase 2.
/// </summary>
public abstract class AgentBase
{
    public string AgentName { get; }

    protected AgentBase(string agentName)
    {
        AgentName = agentName;
    }

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
