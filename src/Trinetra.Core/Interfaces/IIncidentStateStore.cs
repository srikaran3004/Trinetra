using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trinetra.Core.Interfaces;

public class IncidentStateSnapshot
{
    public Guid IncidentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public string? CurrentPhase { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class AgentStateSnapshot
{
    public string AgentName { get; set; } = string.Empty;
    public string Status { get; set; } = "idle"; // idle, running, completed, failed
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputSummary { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public interface IIncidentStateStore
{
    Task SaveIncidentStateAsync(IncidentStateSnapshot snapshot, TimeSpan? expiry = null, CancellationToken ct = default);
    Task<IncidentStateSnapshot?> GetIncidentStateAsync(Guid incidentId, CancellationToken ct = default);
    Task SetAgentStatusAsync(Guid incidentId, AgentStateSnapshot agentState, TimeSpan? expiry = null, CancellationToken ct = default);
    Task<AgentStateSnapshot?> GetAgentStatusAsync(Guid incidentId, string agentName, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, AgentStateSnapshot>> GetAllAgentStatusesAsync(Guid incidentId, CancellationToken ct = default);
    Task DeleteIncidentStateAsync(Guid incidentId, CancellationToken ct = default);
}
