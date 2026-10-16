namespace Trinetra.Core.Enums;

public static class IncidentSeverities
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly string[] All = [Low, Medium, High, Critical];
}

public static class IncidentStatuses
{
    public const string Triggered = "triggered";
    public const string Investigating = "investigating";
    public const string AnalysisComplete = "analysis_complete";
    public const string AwaitingApproval = "awaiting_approval";
    public const string Executing = "executing";
    public const string Monitoring = "monitoring";
    public const string Resolved = "resolved";
    public const string Escalated = "escalated";

    public static readonly string[] All =
    [
        Triggered, Investigating, AnalysisComplete,
        AwaitingApproval, Executing, Monitoring,
        Resolved, Escalated
    ];
}

public static class ExecutionStatuses
{
    public const string Pending = "pending";
    public const string InProgress = "in_progress";
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Timeout = "timeout";

    public static readonly string[] All = [Pending, InProgress, Success, Failed, Timeout];
}

public static class RiskLevels
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";

    public static readonly string[] All = [Low, Medium, High];
}

public static class UserRoles
{
    public const string Admin = "admin";
    public const string Lead = "lead";
    public const string Manager = "manager";
    public const string Sre = "sre";
    public const string Viewer = "viewer";

    public static readonly string[] All = [Admin, Lead, Manager, Sre, Viewer];
}
