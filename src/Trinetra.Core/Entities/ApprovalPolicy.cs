namespace Trinetra.Core.Entities;

public class ApprovalPolicy
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Policy Definition
    public string RuleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; } = 100;

    // Matching Conditions
    public List<string> AppliesToCommands { get; set; } = new();
    public List<string> AppliesToServices { get; set; } = new();
    public string? MinRiskLevel { get; set; } // 'low', 'medium', 'high'
    public List<string> AppliesToIncidentSeverity { get; set; } = new(); // 'critical', 'high', etc.

    // Approval Requirements
    public bool RequiresApproval { get; set; } = false;
    public string? ApprovalLevel { get; set; } // 'lead', 'manager', 'director'
    public int EscalationTimeoutMinutes { get; set; } = 15;
    public string? CanExecuteDuringHours { get; set; } // 'business_hours', 'always'

    // Execution Constraints
    public int MaxConcurrentExecutions { get; set; } = 1;
    public int MaxPerHour { get; set; } = 10;

    // Status
    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? CreatedByUser { get; set; }
}
