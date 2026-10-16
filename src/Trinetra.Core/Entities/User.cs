namespace Trinetra.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "viewer";
    public string? Team { get; set; }
    public string? Department { get; set; }

    public string? SlackUserId { get; set; }
    public string? SlackChannel { get; set; }
    public bool EmailNotifications { get; set; } = true;
    public bool SlackNotifications { get; set; } = true;
    public bool ApprovalNotifications { get; set; } = true;

    public string? ApiKeyHash { get; set; }
    public DateTime? ApiKeyLastUsed { get; set; }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public DateTime? LastPasswordChange { get; set; }

    // Navigation
    public ICollection<Incident> AssignedIncidents { get; set; } = new List<Incident>();
    public ICollection<IncidentEvent> ApprovedEvents { get; set; } = new List<IncidentEvent>();
    public ICollection<IncidentEvent> CreatedEvents { get; set; } = new List<IncidentEvent>();
}
