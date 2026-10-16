using Trinetra.Core.Enums;

namespace Trinetra.Agents.Orchestration;

public class IncidentStateMachine
{
    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        [IncidentStatuses.Triggered] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.Investigating,
            IncidentStatuses.Escalated
        },
        [IncidentStatuses.Investigating] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.AnalysisComplete,
            IncidentStatuses.Escalated
        },
        [IncidentStatuses.AnalysisComplete] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.AwaitingApproval,
            IncidentStatuses.Executing,
            IncidentStatuses.Escalated
        },
        [IncidentStatuses.AwaitingApproval] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.Executing,
            IncidentStatuses.Escalated,
            IncidentStatuses.Resolved
        },
        [IncidentStatuses.Executing] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.Monitoring,
            IncidentStatuses.Escalated,
            IncidentStatuses.Resolved
        },
        [IncidentStatuses.Monitoring] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.Resolved,
            IncidentStatuses.Escalated
        },
        [IncidentStatuses.Resolved] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.Triggered // Can re-open if regression occurs
        },
        [IncidentStatuses.Escalated] = new(StringComparer.OrdinalIgnoreCase)
        {
            IncidentStatuses.Investigating,
            IncidentStatuses.Resolved
        }
    };

    public static bool CanTransition(string currentStatus, string targetStatus)
    {
        if (string.Equals(currentStatus, targetStatus, StringComparison.OrdinalIgnoreCase))
            return true;

        if (AllowedTransitions.TryGetValue(currentStatus, out var validTargets))
        {
            return validTargets.Contains(targetStatus);
        }

        // Unrecognized current status can only transition to escalated
        return string.Equals(targetStatus, IncidentStatuses.Escalated, StringComparison.OrdinalIgnoreCase);
    }

    public static void ValidateTransition(string currentStatus, string targetStatus)
    {
        if (!CanTransition(currentStatus, targetStatus))
        {
            throw new InvalidOperationException($"Invalid incident state transition from '{currentStatus}' to '{targetStatus}'.");
        }
    }
}
