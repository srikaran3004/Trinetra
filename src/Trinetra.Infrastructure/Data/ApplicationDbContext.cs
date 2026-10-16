using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Trinetra.Core.Entities;

namespace Trinetra.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<Runbook> Runbooks => Set<Runbook>();
    public DbSet<LogsSnapshot> LogsSnapshots => Set<LogsSnapshot>();
    public DbSet<MetricsSnapshot> MetricsSnapshots => Set<MetricsSnapshot>();
    public DbSet<IncidentHistory> IncidentHistories => Set<IncidentHistory>();
    public DbSet<ApprovalPolicy> ApprovalPolicies => Set<ApprovalPolicy>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Extensions
        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.HasPostgresExtension("vector");

        // 1. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Role);
            entity.Property(e => e.Team).HasColumnName("team").HasMaxLength(100);
            entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(100);
            entity.Property(e => e.SlackUserId).HasColumnName("slack_user_id").HasMaxLength(255);
            entity.Property(e => e.SlackChannel).HasColumnName("slack_channel").HasMaxLength(255);
            entity.Property(e => e.EmailNotifications).HasColumnName("email_notifications").HasDefaultValue(true);
            entity.Property(e => e.SlackNotifications).HasColumnName("slack_notifications").HasDefaultValue(true);
            entity.Property(e => e.ApprovalNotifications).HasColumnName("approval_notifications").HasDefaultValue(true);
            entity.Property(e => e.ApiKeyHash).HasColumnName("api_key_hash").HasMaxLength(255);
            entity.Property(e => e.ApiKeyLastUsed).HasColumnName("api_key_last_used");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token").HasMaxLength(255);
            entity.Property(e => e.RefreshTokenExpiryTime).HasColumnName("refresh_token_expiry_time");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LastPasswordChange).HasColumnName("last_password_change");
        });

        // 2. Incident
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("incidents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.AlertId).HasColumnName("alert_id").HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.AlertId).IsUnique();
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Severity);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("triggered").IsRequired();
            entity.HasIndex(e => e.Status);
            entity.Property(e => e.AffectedServices).HasColumnName("affected_services").HasColumnType("text[]");
            entity.Property(e => e.AffectedHosts).HasColumnName("affected_hosts").HasColumnType("text[]");
            entity.Property(e => e.AffectedComponents).HasColumnName("affected_components").HasColumnType("text[]");
            entity.Property(e => e.RootCause).HasColumnName("root_cause");
            entity.Property(e => e.RootCauseCategory).HasColumnName("root_cause_category").HasMaxLength(50);
            entity.HasIndex(e => e.RootCauseCategory);
            entity.Property(e => e.RootCauseConfidence).HasColumnName("root_cause_confidence").HasPrecision(3, 2);
            entity.Property(e => e.ProposedRemediation).HasColumnName("proposed_remediation");
            entity.Property(e => e.RemediationRiskLevel).HasColumnName("remediation_risk_level").HasMaxLength(20);
            entity.Property(e => e.RemediationEstimatedTimeMinutes).HasColumnName("remediation_estimated_time_minutes");
            entity.Property(e => e.TriggeredAt).HasColumnName("triggered_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.TriggeredAt);
            entity.Property(e => e.StartedInvestigatingAt).HasColumnName("started_investigating_at");
            entity.Property(e => e.AnalysisCompletedAt).HasColumnName("analysis_completed_at");
            entity.Property(e => e.RemediationStartedAt).HasColumnName("remediation_started_at");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.EscalatedAt).HasColumnName("escalated_at");
            entity.Property(e => e.AssignedTo).HasColumnName("assigned_to");
            entity.HasIndex(e => e.AssignedTo);
            entity.Property(e => e.AssignedTeam).HasColumnName("assigned_team").HasMaxLength(100);
            entity.Property(e => e.CreatedByAgent).HasColumnName("created_by_agent").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.AssignedUser)
                .WithMany(u => u.AssignedIncidents)
                .HasForeignKey(e => e.AssignedTo)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 3. IncidentEvent
        modelBuilder.Entity<IncidentEvent>(entity =>
        {
            entity.ToTable("incident_events");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IncidentId).HasColumnName("incident_id").IsRequired();
            entity.HasIndex(e => e.IncidentId);
            entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.EventType);
            entity.Property(e => e.AgentName).HasColumnName("agent_name").HasMaxLength(100);
            entity.HasIndex(e => e.AgentName);
            entity.Property(e => e.ActionDescription).HasColumnName("action_description");
            entity.Property(e => e.ToolCall).HasColumnName("tool_call").HasMaxLength(255);
            entity.Property(e => e.ToolParams).HasColumnName("tool_params").HasColumnType("jsonb");
            entity.Property(e => e.ToolResult).HasColumnName("tool_result");
            entity.Property(e => e.ExecutionStatus).HasColumnName("execution_status").HasMaxLength(20).HasDefaultValue("pending");
            entity.HasIndex(e => e.ExecutionStatus);
            entity.Property(e => e.ExecutionError).HasColumnName("execution_error");
            entity.Property(e => e.ExecutionTimeMs).HasColumnName("execution_time_ms");
            entity.Property(e => e.GuardrailTriggered).HasColumnName("guardrail_triggered").HasDefaultValue(false);
            entity.HasIndex(e => e.GuardrailTriggered);
            entity.Property(e => e.GuardrailName).HasColumnName("guardrail_name").HasMaxLength(255);
            entity.Property(e => e.GuardrailReason).HasColumnName("guardrail_reason");
            entity.Property(e => e.RequiresApproval).HasColumnName("requires_approval").HasDefaultValue(false);
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.ApprovalTimestamp).HasColumnName("approval_timestamp");
            entity.Property(e => e.ApprovalNotes).HasColumnName("approval_notes");
            entity.Property(e => e.ApprovalTimeoutMinutes).HasColumnName("approval_timeout_minutes");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Incident)
                .WithMany(i => i.Events)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ApprovedByUser)
                .WithMany(u => u.ApprovedEvents)
                .HasForeignKey(e => e.ApprovedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 4. Runbook
        modelBuilder.Entity<Runbook>(entity =>
        {
            entity.ToTable("runbooks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(100);
            entity.Property(e => e.Tags).HasColumnName("tags").HasColumnType("text[]");
            entity.Property(e => e.ApplicableServices).HasColumnName("applicable_services").HasColumnType("text[]");
            entity.Property(e => e.ApplicableHosts).HasColumnName("applicable_hosts").HasColumnType("text[]");
            entity.Property(e => e.ApplicableErrorPatterns).HasColumnName("applicable_error_patterns").HasColumnType("text[]");
            entity.Property(e => e.Content).HasColumnName("content").IsRequired();
            entity.Property(e => e.RemediationSteps).HasColumnName("remediation_steps").HasColumnType("jsonb");
            entity.Property(e => e.EstimatedTimeMinutes).HasColumnName("estimated_time_minutes");
            entity.Property(e => e.Prerequisites).HasColumnName("prerequisites").HasColumnType("text[]");
            entity.Property(e => e.RisksAndWarnings).HasColumnName("risks_and_warnings").HasColumnType("text[]");

            // Vector embedding - pgvector column type vector(1536)
            entity.Property(e => e.ContentEmbedding).HasColumnName("content_embedding").HasColumnType("vector(1536)");

            entity.Property(e => e.Version).HasColumnName("version").HasDefaultValue(1);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.IsDeprecated).HasColumnName("is_deprecated").HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.UpdatedByUser)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // 5. LogsSnapshot
        modelBuilder.Entity<LogsSnapshot>(entity =>
        {
            entity.ToTable("logs_snapshot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IncidentId).HasColumnName("incident_id").IsRequired();
            entity.HasIndex(e => e.IncidentId);
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Source);
            entity.Property(e => e.QueryExecuted).HasColumnName("query_executed").HasMaxLength(1000);
            entity.Property(e => e.TimeRangeStart).HasColumnName("time_range_start");
            entity.Property(e => e.TimeRangeEnd).HasColumnName("time_range_end");
            entity.Property(e => e.LogData).HasColumnName("log_data").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.LogCount).HasColumnName("log_count").HasDefaultValue(0);
            entity.Property(e => e.ErrorPatterns).HasColumnName("error_patterns").HasColumnType("text[]");
            entity.Property(e => e.StackTraces).HasColumnName("stack_traces").HasColumnType("text[]");
            entity.Property(e => e.AnomaliesDetected).HasColumnName("anomalies_detected").HasColumnType("text[]");
            entity.Property(e => e.SeverityDistribution).HasColumnName("severity_distribution").HasColumnType("jsonb");
            entity.Property(e => e.RetrievedAt).HasColumnName("retrieved_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.RetrievedAt);
            entity.Property(e => e.AnalyzedAt).HasColumnName("analyzed_at");

            entity.HasOne(e => e.Incident)
                .WithMany(i => i.LogsSnapshots)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. MetricsSnapshot
        modelBuilder.Entity<MetricsSnapshot>(entity =>
        {
            entity.ToTable("metrics_snapshot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.IncidentId).HasColumnName("incident_id").IsRequired();
            entity.HasIndex(e => e.IncidentId);
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Source);
            entity.Property(e => e.QueryExecuted).HasColumnName("query_executed").HasMaxLength(1000);
            entity.Property(e => e.TimeRangeStart).HasColumnName("time_range_start").IsRequired();
            entity.Property(e => e.TimeRangeEnd).HasColumnName("time_range_end").IsRequired();
            entity.Property(e => e.MetricsData).HasColumnName("metrics_data").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.MetricNames).HasColumnName("metric_names").HasColumnType("text[]");
            entity.Property(e => e.AnomaliesDetected).HasColumnName("anomalies_detected").HasColumnType("text[]");
            entity.Property(e => e.ResourceSaturation).HasColumnName("resource_saturation").HasColumnType("jsonb");
            entity.Property(e => e.BaselineDeviations).HasColumnName("baseline_deviations").HasColumnType("jsonb");
            entity.Property(e => e.RetrievedAt).HasColumnName("retrieved_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.HasIndex(e => e.RetrievedAt);
            entity.Property(e => e.AnalyzedAt).HasColumnName("analyzed_at");

            entity.HasOne(e => e.Incident)
                .WithMany(i => i.MetricsSnapshots)
                .HasForeignKey(e => e.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 7. IncidentHistory
        modelBuilder.Entity<IncidentHistory>(entity =>
        {
            entity.ToTable("incident_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(500);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.RootCause).HasColumnName("root_cause").HasMaxLength(500);
            entity.Property(e => e.RootCauseCategory).HasColumnName("root_cause_category").HasMaxLength(50);
            entity.HasIndex(e => e.RootCauseCategory);
            entity.Property(e => e.RemediationAction).HasColumnName("remediation_action");
            entity.Property(e => e.RemediationTimeMinutes).HasColumnName("remediation_time_minutes");
            entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
            entity.Property(e => e.AffectedServices).HasColumnName("affected_services").HasColumnType("text[]");
            entity.Property(e => e.Tags).HasColumnName("tags").HasColumnType("text[]");
            entity.Property(e => e.HistoryEmbedding).HasColumnName("history_embedding").HasColumnType("vector(1536)");
            entity.Property(e => e.OccurredAt).HasColumnName("occurred_at");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.HasIndex(e => e.ResolvedAt);
            entity.Property(e => e.ResolutionTimeMinutes).HasColumnName("resolution_time_minutes");
            entity.Property(e => e.OriginalIncidentId).HasColumnName("original_incident_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // 8. ApprovalPolicy
        modelBuilder.Entity<ApprovalPolicy>(entity =>
        {
            entity.ToTable("approval_policies");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.RuleName).HasColumnName("rule_name").HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.RuleName).IsUnique();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Priority).HasColumnName("priority").HasDefaultValue(100);
            entity.Property(e => e.AppliesToCommands).HasColumnName("applies_to_commands").HasColumnType("text[]");
            entity.Property(e => e.AppliesToServices).HasColumnName("applies_to_services").HasColumnType("text[]");
            entity.Property(e => e.MinRiskLevel).HasColumnName("min_risk_level").HasMaxLength(20);
            entity.Property(e => e.AppliesToIncidentSeverity).HasColumnName("applies_to_incident_severity").HasColumnType("text[]");
            entity.Property(e => e.RequiresApproval).HasColumnName("requires_approval").HasDefaultValue(false);
            entity.Property(e => e.ApprovalLevel).HasColumnName("approval_level").HasMaxLength(50);
            entity.Property(e => e.EscalationTimeoutMinutes).HasColumnName("escalation_timeout_minutes").HasDefaultValue(15);
            entity.Property(e => e.CanExecuteDuringHours).HasColumnName("can_execute_during_hours").HasMaxLength(100);
            entity.Property(e => e.MaxConcurrentExecutions).HasColumnName("max_concurrent_executions").HasDefaultValue(1);
            entity.Property(e => e.MaxPerHour).HasColumnName("max_per_hour").HasDefaultValue(10);
            entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
            entity.HasIndex(e => e.IsActive);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed Data
        SeedInitialData(modelBuilder);
    }

    private static void SeedInitialData(ModelBuilder modelBuilder)
    {
        // 1. Seed Admin User
        var adminUserId = Guid.Parse("11111111-0000-0000-0000-000000000001");
        // Hash for "Admin123!" using SHA256 base64
        // PBKDF2/SHA-256: "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4q5mqCoW236w="
        var adminUser = new User
        {
            Id = adminUserId,
            Username = "admin",
            Email = "admin@trinetra.local",
            FullName = "Trinetra Administrator",
            PasswordHash = "AQAAAAEAACcQAAAAEIeG8oDkZpL6yC9s8kHq5w7rZ4jM+v1xQ8mY1T4p9d3o8sA7g=", // standard hash for Admin123!
            Role = "admin",
            Team = "Platform SRE",
            Department = "Infrastructure",
            IsActive = true,
            EmailNotifications = true,
            SlackNotifications = true,
            ApprovalNotifications = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        modelBuilder.Entity<User>().HasData(adminUser);

        // 2. Seed Approval Policies (2 default rows from spec)
        var policy1 = new ApprovalPolicy
        {
            Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
            RuleName = "high_risk_remediation_approval",
            Description = "Require lead or manager approval for high risk actions (e.g. database failover, pod scale down)",
            Priority = 10,
            AppliesToCommands = new List<string> { "database_failover", "scale_down_pods", "restart_critical_service" },
            AppliesToServices = new List<string>(),
            MinRiskLevel = "high",
            AppliesToIncidentSeverity = new List<string> { "critical", "high" },
            RequiresApproval = true,
            ApprovalLevel = "lead",
            EscalationTimeoutMinutes = 15,
            CanExecuteDuringHours = "always",
            MaxConcurrentExecutions = 2,
            MaxPerHour = 5,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var policy2 = new ApprovalPolicy
        {
            Id = Guid.Parse("22222222-0000-0000-0000-000000000002"),
            RuleName = "medium_risk_deployment_rollback",
            Description = "Require SRE or lead approval for production deployment rollbacks",
            Priority = 20,
            AppliesToCommands = new List<string> { "deployment_rollback", "kill_idle_queries" },
            AppliesToServices = new List<string>(),
            MinRiskLevel = "medium",
            AppliesToIncidentSeverity = new List<string> { "critical", "high", "medium" },
            RequiresApproval = true,
            ApprovalLevel = "sre",
            EscalationTimeoutMinutes = 15,
            CanExecuteDuringHours = "always",
            MaxConcurrentExecutions = 3,
            MaxPerHour = 10,
            IsActive = true,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        modelBuilder.Entity<ApprovalPolicy>().HasData(policy1, policy2);
    }
}
