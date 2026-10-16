using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;
using Trinetra.Infrastructure.Data;

#nullable disable

namespace Trinetra.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "uuid-ossp");
            NpgsqlModelBuilderExtensions.HasPostgresExtension(modelBuilder, "vector");
            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Trinetra.Core.Entities.ApprovalPolicy", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("ApprovalLevel")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("approval_level");

                    b.Property<List<string>>("AppliesToCommands")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("applies_to_commands");

                    b.Property<List<string>>("AppliesToIncidentSeverity")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("applies_to_incident_severity");

                    b.Property<List<string>>("AppliesToServices")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("applies_to_services");

                    b.Property<string>("CanExecuteDuringHours")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("can_execute_during_hours");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<int>("EscalationTimeoutMinutes")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(15)
                        .HasColumnName("escalation_timeout_minutes");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<int>("MaxConcurrentExecutions")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(1)
                        .HasColumnName("max_concurrent_executions");

                    b.Property<int>("MaxPerHour")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(10)
                        .HasColumnName("max_per_hour");

                    b.Property<string>("MinRiskLevel")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("min_risk_level");

                    b.Property<int>("Priority")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(100)
                        .HasColumnName("priority");

                    b.Property<bool>("RequiresApproval")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("requires_approval");

                    b.Property<string>("RuleName")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("rule_name");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");
                    b.HasIndex("IsActive");
                    b.HasIndex("RuleName").IsUnique();

                    b.ToTable("approval_policies", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new Guid("22222222-0000-0000-0000-000000000001"),
                            AppliesToCommands = new List<string> { "database_failover", "scale_down_pods", "restart_critical_service" },
                            AppliesToIncidentSeverity = new List<string> { "critical", "high" },
                            AppliesToServices = new List<string>(),
                            ApprovalLevel = "lead",
                            CanExecuteDuringHours = "always",
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Require lead or manager approval for high risk actions (e.g. database failover, pod scale down)",
                            EscalationTimeoutMinutes = 15,
                            IsActive = true,
                            MaxConcurrentExecutions = 2,
                            MaxPerHour = 5,
                            MinRiskLevel = "high",
                            Priority = 10,
                            RequiresApproval = true,
                            RuleName = "high_risk_remediation_approval",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        },
                        new
                        {
                            Id = new Guid("22222222-0000-0000-0000-000000000002"),
                            AppliesToCommands = new List<string> { "deployment_rollback", "kill_idle_queries" },
                            AppliesToIncidentSeverity = new List<string> { "critical", "high", "medium" },
                            AppliesToServices = new List<string>(),
                            ApprovalLevel = "sre",
                            CanExecuteDuringHours = "always",
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Description = "Require SRE or lead approval for production deployment rollbacks",
                            EscalationTimeoutMinutes = 15,
                            IsActive = true,
                            MaxConcurrentExecutions = 3,
                            MaxPerHour = 10,
                            MinRiskLevel = "medium",
                            Priority = 20,
                            RequiresApproval = true,
                            RuleName = "medium_risk_deployment_rollback",
                            UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        });
                });

            modelBuilder.Entity("Trinetra.Core.Entities.Incident", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<List<string>>("AffectedComponents")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("affected_components");

                    b.Property<List<string>>("AffectedHosts")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("affected_hosts");

                    b.Property<List<string>>("AffectedServices")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("affected_services");

                    b.Property<string>("AlertId")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("alert_id");

                    b.Property<DateTime?>("AnalysisCompletedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("analysis_completed_at");

                    b.Property<string>("AssignedTeam")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("assigned_team");

                    b.Property<Guid?>("AssignedTo")
                        .HasColumnType("uuid")
                        .HasColumnName("assigned_to");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("CreatedByAgent")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("created_by_agent");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<DateTime?>("EscalatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("escalated_at");

                    b.Property<string>("ProposedRemediation")
                        .HasColumnType("text")
                        .HasColumnName("proposed_remediation");

                    b.Property<int?>("RemediationEstimatedTimeMinutes")
                        .HasColumnType("integer")
                        .HasColumnName("remediation_estimated_time_minutes");

                    b.Property<string>("RemediationRiskLevel")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("remediation_risk_level");

                    b.Property<DateTime?>("RemediationStartedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("remediation_started_at");

                    b.Property<DateTime?>("ResolvedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("resolved_at");

                    b.Property<string>("RootCause")
                        .HasColumnType("text")
                        .HasColumnName("root_cause");

                    b.Property<string>("RootCauseCategory")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("root_cause_category");

                    b.Property<decimal?>("RootCauseConfidence")
                        .HasPrecision(3, 2)
                        .HasColumnType("numeric(3,2)")
                        .HasColumnName("root_cause_confidence");

                    b.Property<string>("Severity")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("severity");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("source");

                    b.Property<DateTime?>("StartedInvestigatingAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("started_investigating_at");

                    b.Property<string>("Status")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasDefaultValue("triggered")
                        .HasColumnName("status");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("title");

                    b.Property<DateTime>("TriggeredAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("triggered_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.HasKey("Id");

                    b.HasIndex("AlertId").IsUnique();
                    b.HasIndex("AssignedTo");
                    b.HasIndex("RootCauseCategory");
                    b.HasIndex("Severity");
                    b.HasIndex("Status");
                    b.HasIndex("TriggeredAt");

                    b.ToTable("incidents", (string)null);
                });

            modelBuilder.Entity("Trinetra.Core.Entities.IncidentEvent", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("ActionDescription")
                        .HasColumnType("text")
                        .HasColumnName("action_description");

                    b.Property<string>("AgentName")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("agent_name");

                    b.Property<string>("ApprovalNotes")
                        .HasColumnType("text")
                        .HasColumnName("approval_notes");

                    b.Property<int?>("ApprovalTimeoutMinutes")
                        .HasColumnType("integer")
                        .HasColumnName("approval_timeout_minutes");

                    b.Property<DateTime?>("ApprovalTimestamp")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("approval_timestamp");

                    b.Property<Guid?>("ApprovedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("approved_by");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<Guid?>("CreatedByUserId")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by_user_id");

                    b.Property<string>("EventType")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("event_type");

                    b.Property<string>("ExecutionError")
                        .HasColumnType("text")
                        .HasColumnName("execution_error");

                    b.Property<string>("ExecutionStatus")
                        .IsRequired()
                        .ValueGeneratedOnAdd()
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasDefaultValue("pending")
                        .HasColumnName("execution_status");

                    b.Property<int?>("ExecutionTimeMs")
                        .HasColumnType("integer")
                        .HasColumnName("execution_time_ms");

                    b.Property<string>("GuardrailName")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("guardrail_name");

                    b.Property<string>("GuardrailReason")
                        .HasColumnType("text")
                        .HasColumnName("guardrail_reason");

                    b.Property<bool>("GuardrailTriggered")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("guardrail_triggered");

                    b.Property<Guid>("IncidentId")
                        .HasColumnType("uuid")
                        .HasColumnName("incident_id");

                    b.Property<bool>("RequiresApproval")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("requires_approval");

                    b.Property<string>("ToolCall")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("tool_call");

                    b.Property<string>("ToolParams")
                        .HasColumnType("jsonb")
                        .HasColumnName("tool_params");

                    b.Property<string>("ToolResult")
                        .HasColumnType("text")
                        .HasColumnName("tool_result");

                    b.HasKey("Id");

                    b.HasIndex("AgentName");
                    b.HasIndex("ApprovedBy");
                    b.HasIndex("CreatedAt");
                    b.HasIndex("CreatedByUserId");
                    b.HasIndex("EventType");
                    b.HasIndex("ExecutionStatus");
                    b.HasIndex("GuardrailTriggered");
                    b.HasIndex("IncidentId");

                    b.ToTable("incident_events", (string)null);
                });

            modelBuilder.Entity("Trinetra.Core.Entities.IncidentHistory", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<List<string>>("AffectedServices")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("affected_services");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<Vector>("HistoryEmbedding")
                        .HasColumnType("vector(1536)")
                        .HasColumnName("history_embedding");

                    b.Property<DateTime?>("OccurredAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("occurred_at");

                    b.Property<Guid?>("OriginalIncidentId")
                        .HasColumnType("uuid")
                        .HasColumnName("original_incident_id");

                    b.Property<string>("RemediationAction")
                        .HasColumnType("text")
                        .HasColumnName("remediation_action");

                    b.Property<int?>("RemediationTimeMinutes")
                        .HasColumnType("integer")
                        .HasColumnName("remediation_time_minutes");

                    b.Property<int?>("ResolutionTimeMinutes")
                        .HasColumnType("integer")
                        .HasColumnName("resolution_time_minutes");

                    b.Property<DateTime?>("ResolvedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("resolved_at");

                    b.Property<string>("RootCause")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("root_cause");

                    b.Property<string>("RootCauseCategory")
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("root_cause_category");

                    b.Property<string>("Severity")
                        .HasMaxLength(20)
                        .HasColumnType("character varying(20)")
                        .HasColumnName("severity");

                    b.Property<List<string>>("Tags")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("tags");

                    b.Property<string>("Title")
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("title");

                    b.HasKey("Id");

                    b.HasIndex("ResolvedAt");
                    b.HasIndex("RootCauseCategory");

                    b.ToTable("incident_history", (string)null);
                });

            modelBuilder.Entity("Trinetra.Core.Entities.LogsSnapshot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<DateTime?>("AnalyzedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("analyzed_at");

                    b.Property<List<string>>("AnomaliesDetected")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("anomalies_detected");

                    b.Property<List<string>>("ErrorPatterns")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("error_patterns");

                    b.Property<Guid>("IncidentId")
                        .HasColumnType("uuid")
                        .HasColumnName("incident_id");

                    b.Property<int>("LogCount")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(0)
                        .HasColumnName("log_count");

                    b.Property<string>("LogData")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("log_data");

                    b.Property<string>("QueryExecuted")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)")
                        .HasColumnName("query_executed");

                    b.Property<DateTime>("RetrievedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("retrieved_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("SeverityDistribution")
                        .HasColumnType("jsonb")
                        .HasColumnName("severity_distribution");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("source");

                    b.Property<List<string>>("StackTraces")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("stack_traces");

                    b.Property<DateTime?>("TimeRangeEnd")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("time_range_end");

                    b.Property<DateTime?>("TimeRangeStart")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("time_range_start");

                    b.HasKey("Id");

                    b.HasIndex("IncidentId");
                    b.HasIndex("RetrievedAt");
                    b.HasIndex("Source");

                    b.ToTable("logs_snapshot", (string)null);
                });

            modelBuilder.Entity("Trinetra.Core.Entities.MetricsSnapshot", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<DateTime?>("AnalyzedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("analyzed_at");

                    b.Property<List<string>>("AnomaliesDetected")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("anomalies_detected");

                    b.Property<string>("BaselineDeviations")
                        .HasColumnType("jsonb")
                        .HasColumnName("baseline_deviations");

                    b.Property<Guid>("IncidentId")
                        .HasColumnType("uuid")
                        .HasColumnName("incident_id");

                    b.Property<List<string>>("MetricNames")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("metric_names");

                    b.Property<string>("MetricsData")
                        .IsRequired()
                        .HasColumnType("jsonb")
                        .HasColumnName("metrics_data");

                    b.Property<string>("QueryExecuted")
                        .HasMaxLength(1000)
                        .HasColumnType("character varying(1000)")
                        .HasColumnName("query_executed");

                    b.Property<string>("ResourceSaturation")
                        .HasColumnType("jsonb")
                        .HasColumnName("resource_saturation");

                    b.Property<DateTime>("RetrievedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("retrieved_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("source");

                    b.Property<DateTime>("TimeRangeEnd")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("time_range_end");

                    b.Property<DateTime>("TimeRangeStart")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("time_range_start");

                    b.HasKey("Id");

                    b.HasIndex("IncidentId");
                    b.HasIndex("RetrievedAt");
                    b.HasIndex("Source");

                    b.ToTable("metrics_snapshot", (string)null);
                });

            modelBuilder.Entity("Trinetra.Core.Entities.Runbook", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<List<string>>("ApplicableErrorPatterns")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("applicable_error_patterns");

                    b.Property<List<string>>("ApplicableHosts")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("applicable_hosts");

                    b.Property<List<string>>("ApplicableServices")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("applicable_services");

                    b.Property<string>("Category")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("category");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("content");

                    b.Property<Vector>("ContentEmbedding")
                        .HasColumnType("vector(1536)")
                        .HasColumnName("content_embedding");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<Guid?>("CreatedBy")
                        .HasColumnType("uuid")
                        .HasColumnName("created_by");

                    b.Property<string>("Description")
                        .HasColumnType("text")
                        .HasColumnName("description");

                    b.Property<int?>("EstimatedTimeMinutes")
                        .HasColumnType("integer")
                        .HasColumnName("estimated_time_minutes");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<bool>("IsDeprecated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("is_deprecated");

                    b.Property<List<string>>("Prerequisites")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("prerequisites");

                    b.Property<string>("RemediationSteps")
                        .HasColumnType("jsonb")
                        .HasColumnName("remediation_steps");

                    b.Property<List<string>>("RisksAndWarnings")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("risks_and_warnings");

                    b.Property<List<string>>("Tags")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("tags");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(500)
                        .HasColumnType("character varying(500)")
                        .HasColumnName("title");

                    b.Property<DateTime>("UpdatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<int>("Version")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(1)
                        .HasColumnName("version");

                    b.HasKey("Id");

                    b.HasIndex("CreatedBy");
                    b.HasIndex("IsActive");
                    b.HasIndex("UpdatedBy");

                    b.ToTable("runbooks", (string)null);
                });

            modelBuilder.Entity("Trinetra.Core.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("ApiKeyHash")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("api_key_hash");

                    b.Property<DateTime?>("ApiKeyLastUsed")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("api_key_last_used");

                    b.Property<bool>("ApprovalNotifications")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("approval_notifications");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    b.Property<string>("Department")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("department");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("email");

                    b.Property<bool>("EmailNotifications")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("email_notifications");

                    b.Property<string>("FullName")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("full_name");

                    b.Property<bool>("IsActive")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("is_active");

                    b.Property<DateTime?>("LastLogin")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_login");

                    b.Property<DateTime?>("LastPasswordChange")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("last_password_change");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("password_hash");

                    b.Property<string>("RefreshToken")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("refresh_token");

                    b.Property<DateTime?>("RefreshTokenExpiryTime")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("refresh_token_expiry_time");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("character varying(50)")
                        .HasColumnName("role");

                    b.Property<string>("SlackChannel")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("slack_channel");

                    b.Property<bool>("SlackNotifications")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(true)
                        .HasColumnName("slack_notifications");

                    b.Property<string>("SlackUserId")
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("slack_user_id");

                    b.Property<string>("Team")
                        .HasMaxLength(100)
                        .HasColumnType("character varying(100)")
                        .HasColumnName("team");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(255)
                        .HasColumnType("character varying(255)")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.HasIndex("Email").IsUnique();
                    b.HasIndex("IsActive");
                    b.HasIndex("Role");
                    b.HasIndex("Username").IsUnique();

                    b.ToTable("users", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new Guid("11111111-0000-0000-0000-000000000001"),
                            ApprovalNotifications = true,
                            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                            Department = "Infrastructure",
                            Email = "admin@trinetra.local",
                            EmailNotifications = true,
                            FullName = "Trinetra Administrator",
                            IsActive = true,
                            PasswordHash = "AQAAAAEAACcQAAAAEIeG8oDkZpL6yC9s8kHq5w7rZ4jM+v1xQ8mY1T4p9d3o8sA7g=",
                            Role = "admin",
                            SlackNotifications = true,
                            Team = "Platform SRE",
                            Username = "admin"
                        });
                });

            modelBuilder.Entity("Trinetra.Core.Entities.ApprovalPolicy", b =>
                {
                    b.HasOne("Trinetra.Core.Entities.User", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("CreatedByUser");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.Incident", b =>
                {
                    b.HasOne("Trinetra.Core.Entities.User", "AssignedUser")
                        .WithMany("AssignedIncidents")
                        .HasForeignKey("AssignedTo")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("AssignedUser");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.IncidentEvent", b =>
                {
                    b.HasOne("Trinetra.Core.Entities.User", "ApprovedByUser")
                        .WithMany("ApprovedEvents")
                        .HasForeignKey("ApprovedBy")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Trinetra.Core.Entities.User", "CreatedByUser")
                        .WithMany("CreatedEvents")
                        .HasForeignKey("CreatedByUserId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Trinetra.Core.Entities.Incident", "Incident")
                        .WithMany("Events")
                        .HasForeignKey("IncidentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ApprovedByUser");
                    b.Navigation("CreatedByUser");
                    b.Navigation("Incident");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.LogsSnapshot", b =>
                {
                    b.HasOne("Trinetra.Core.Entities.Incident", "Incident")
                        .WithMany("LogsSnapshots")
                        .HasForeignKey("IncidentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Incident");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.MetricsSnapshot", b =>
                {
                    b.HasOne("Trinetra.Core.Entities.Incident", "Incident")
                        .WithMany("MetricsSnapshots")
                        .HasForeignKey("IncidentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Incident");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.Runbook", b =>
                {
                    b.HasOne("Trinetra.Core.Entities.User", "CreatedByUser")
                        .WithMany()
                        .HasForeignKey("CreatedBy")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Trinetra.Core.Entities.User", "UpdatedByUser")
                        .WithMany()
                        .HasForeignKey("UpdatedBy")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("CreatedByUser");
                    b.Navigation("UpdatedByUser");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.Incident", b =>
                {
                    b.Navigation("Events");
                    b.Navigation("LogsSnapshots");
                    b.Navigation("MetricsSnapshots");
                });

            modelBuilder.Entity("Trinetra.Core.Entities.User", b =>
                {
                    b.Navigation("ApprovedEvents");
                    b.Navigation("AssignedIncidents");
                    b.Navigation("CreatedEvents");
                });
#pragma warning restore 612, 618
        }
    }
}
