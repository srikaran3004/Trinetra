using Microsoft.AspNetCore.SignalR;

namespace Trinetra.API.Hubs;

public interface IIncidentClient
{
    Task IncidentCreated(object incident);
    Task IncidentStatusChanged(object statusChange);
    Task AgentActionCompleted(object action);
    Task ApprovalRequired(object approval);
}

public class IncidentHub : Hub<IIncidentClient>
{
    public async Task JoinIncidentGroup(string incidentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"incident_{incidentId}");
    }

    public async Task LeaveIncidentGroup(string incidentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"incident_{incidentId}");
    }
}
