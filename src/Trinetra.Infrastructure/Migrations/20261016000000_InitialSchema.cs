using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace Trinetra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    team = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    slack_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    slack_channel = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    slack_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    approval_notifications = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    api_key_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    api_key_last_used = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refresh_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    refresh_token_expiry_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_password_change = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "incident_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    root_cause = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    root_cause_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    remediation_action = table.Column<string>(type: "text", nullable: true),
                    remediation_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    affected_services = table.Column<List<string>>(type: "text[]", nullable: false),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    history_embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolution_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    original_incident_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "approval_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    rule_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    applies_to_commands = table.Column<List<string>>(type: "text[]", nullable: false),
                    applies_to_services = table.Column<List<string>>(type: "text[]", nullable: false),
                    min_risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    applies_to_incident_severity = table.Column<List<string>>(type: "text[]", nullable: false),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    approval_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    escalation_timeout_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                    can_execute_during_hours = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    max_concurrent_executions = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_per_hour = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_policies", x => x.id);
                    table.ForeignKey(
                        name: "FK_approval_policies_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    alert_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "triggered"),
                    affected_services = table.Column<List<string>>(type: "text[]", nullable: false),
                    affected_hosts = table.Column<List<string>>(type: "text[]", nullable: false),
                    affected_components = table.Column<List<string>>(type: "text[]", nullable: false),
                    root_cause = table.Column<string>(type: "text", nullable: true),
                    root_cause_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    root_cause_confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    proposed_remediation = table.Column<string>(type: "text", nullable: true),
                    remediation_risk_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    remediation_estimated_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    triggered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    started_investigating_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    analysis_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    remediation_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_team = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_agent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.id);
                    table.ForeignKey(
                        name: "FK_incidents_users_assigned_to",
                        column: x => x.assigned_to,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "runbooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    applicable_services = table.Column<List<string>>(type: "text[]", nullable: false),
                    applicable_hosts = table.Column<List<string>>(type: "text[]", nullable: false),
                    applicable_error_patterns = table.Column<List<string>>(type: "text[]", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    remediation_steps = table.Column<string>(type: "jsonb", nullable: true),
                    estimated_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    prerequisites = table.Column<List<string>>(type: "text[]", nullable: false),
                    risks_and_warnings = table.Column<List<string>>(type: "text[]", nullable: false),
                    content_embedding = table.Column<Vector>(type: "vector(1536)", nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_deprecated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_runbooks", x => x.id);
                    table.ForeignKey(
                        name: "FK_runbooks_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_runbooks_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "incident_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    agent_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    action_description = table.Column<string>(type: "text", nullable: true),
                    tool_call = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    tool_params = table.Column<string>(type: "jsonb", nullable: true),
                    tool_result = table.Column<string>(type: "text", nullable: true),
                    execution_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    execution_error = table.Column<string>(type: "text", nullable: true),
                    execution_time_ms = table.Column<int>(type: "integer", nullable: true),
                    guardrail_triggered = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    guardrail_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    guardrail_reason = table.Column<string>(type: "text", nullable: true),
                    requires_approval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approval_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    approval_notes = table.Column<string>(type: "text", nullable: true),
                    approval_timeout_minutes = table.Column<int>(type: "integer", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_events_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_incident_events_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_incident_events_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "logs_snapshot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    query_executed = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time_range_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    time_range_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    log_data = table.Column<string>(type: "jsonb", nullable: false),
                    log_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_patterns = table.Column<List<string>>(type: "text[]", nullable: false),
                    stack_traces = table.Column<List<string>>(type: "text[]", nullable: false),
                    anomalies_detected = table.Column<List<string>>(type: "text[]", nullable: false),
                    severity_distribution = table.Column<string>(type: "jsonb", nullable: true),
                    retrieved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_logs_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_logs_snapshot_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metrics_snapshot",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    query_executed = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    time_range_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    time_range_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    metrics_data = table.Column<string>(type: "jsonb", nullable: false),
                    metric_names = table.Column<List<string>>(type: "text[]", nullable: false),
                    anomalies_detected = table.Column<List<string>>(type: "text[]", nullable: false),
                    resource_saturation = table.Column<string>(type: "jsonb", nullable: true),
                    baseline_deviations = table.Column<string>(type: "jsonb", nullable: true),
                    retrieved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metrics_snapshot", x => x.id);
                    table.ForeignKey(
                        name: "FK_metrics_snapshot_incidents_incident_id",
                        column: x => x.incident_id,
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "api_key_hash", "api_key_last_used", "approval_notifications", "created_at", "department", "email", "email_notifications", "full_name", "is_active", "last_login", "last_password_change", "password_hash", "refresh_token", "refresh_token_expiry_time", "role", "slack_channel", "slack_notifications", "slack_user_id", "team", "username" },
                values: new object[] { new Guid("11111111-0000-0000-0000-000000000001"), null, null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Infrastructure", "admin@trinetra.local", true, "Trinetra Administrator", true, null, null, "AQAAAAEAACcQAAAAEIeG8oDkZpL6yC9s8kHq5w7rZ4jM+v1xQ8mY1T4p9d3o8sA7g=", null, null, "admin", null, true, null, "Platform SRE", "admin" });

            migrationBuilder.InsertData(
                table: "approval_policies",
                columns: new[] { "id", "applies_to_commands", "applies_to_incident_severity", "applies_to_services", "approval_level", "can_execute_during_hours", "created_at", "created_by", "description", "escalation_timeout_minutes", "is_active", "max_concurrent_executions", "max_per_hour", "min_risk_level", "priority", "requires_approval", "rule_name", "updated_at" },
                values: new object[,]
                {
                    { new Guid("22222222-0000-0000-0000-000000000001"), new List<string> { "database_failover", "scale_down_pods", "restart_critical_service" }, new List<string> { "critical", "high" }, new List<string>(), "lead", "always", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Require lead or manager approval for high risk actions (e.g. database failover, pod scale down)", 15, true, 2, 5, "high", 10, true, "high_risk_remediation_approval", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("22222222-0000-0000-0000-000000000002"), new List<string> { "deployment_rollback", "kill_idle_queries" }, new List<string> { "critical", "high", "medium" }, new List<string>(), "sre", "always", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Require SRE or lead approval for production deployment rollbacks", 15, true, 3, 10, "medium", 20, true, "medium_risk_deployment_rollback", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_approval_policies_created_by",
                table: "approval_policies",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_approval_policies_is_active",
                table: "approval_policies",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_approval_policies_rule_name",
                table: "approval_policies",
                column: "rule_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_agent_name",
                table: "incident_events",
                column: "agent_name");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_approved_by",
                table: "incident_events",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_created_at",
                table: "incident_events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_created_by_user_id",
                table: "incident_events",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_event_type",
                table: "incident_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_execution_status",
                table: "incident_events",
                column: "execution_status");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_guardrail_triggered",
                table: "incident_events",
                column: "guardrail_triggered");

            migrationBuilder.CreateIndex(
                name: "IX_incident_events_incident_id",
                table: "incident_events",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_incident_history_resolved_at",
                table: "incident_history",
                column: "resolved_at");

            migrationBuilder.CreateIndex(
                name: "IX_incident_history_root_cause_category",
                table: "incident_history",
                column: "root_cause_category");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_alert_id",
                table: "incidents",
                column: "alert_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_incidents_assigned_to",
                table: "incidents",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_root_cause_category",
                table: "incidents",
                column: "root_cause_category");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_severity",
                table: "incidents",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_status",
                table: "incidents",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_incidents_triggered_at",
                table: "incidents",
                column: "triggered_at");

            migrationBuilder.CreateIndex(
                name: "IX_logs_snapshot_incident_id",
                table: "logs_snapshot",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_logs_snapshot_retrieved_at",
                table: "logs_snapshot",
                column: "retrieved_at");

            migrationBuilder.CreateIndex(
                name: "IX_logs_snapshot_source",
                table: "logs_snapshot",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_snapshot_incident_id",
                table: "metrics_snapshot",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_snapshot_retrieved_at",
                table: "metrics_snapshot",
                column: "retrieved_at");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_snapshot_source",
                table: "metrics_snapshot",
                column: "source");

            migrationBuilder.CreateIndex(
                name: "IX_runbooks_created_by",
                table: "runbooks",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_runbooks_is_active",
                table: "runbooks",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_runbooks_updated_by",
                table: "runbooks",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_is_active",
                table: "users",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_policies");

            migrationBuilder.DropTable(
                name: "incident_events");

            migrationBuilder.DropTable(
                name: "incident_history");

            migrationBuilder.DropTable(
                name: "logs_snapshot");

            migrationBuilder.DropTable(
                name: "metrics_snapshot");

            migrationBuilder.DropTable(
                name: "runbooks");

            migrationBuilder.DropTable(
                name: "incidents");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
