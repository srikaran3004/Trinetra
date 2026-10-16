using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Coordinator;
using Trinetra.Agents.Factory;
using Trinetra.Agents.Orchestration;
using Trinetra.API.Hubs;
using Trinetra.API.Services;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Auth;
using Trinetra.Infrastructure.Data;
using Trinetra.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Setup
var esUrl = builder.Configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
var loggerConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "Trinetra.API")
    .WriteTo.Console();

if (!string.IsNullOrWhiteSpace(esUrl) && Uri.TryCreate(esUrl, UriKind.Absolute, out var esUri))
{
    loggerConfig.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(esUri)
    {
        AutoRegisterTemplate = true,
        AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv8,
        IndexFormat = "trinetra-logs-{0:yyyy.MM.dd}",
        NumberOfReplicas = 0,
        NumberOfShards = 1,
        FailureCallback = (logEvent, ex) => Console.WriteLine($"Unable to submit event to Elasticsearch: {logEvent.MessageTemplate}"),
        EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog | EmitEventFailureHandling.WriteToFailureSink
    });
}

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

// 2. OpenTelemetry Tracing
var otelEndpoint = builder.Configuration["Otel:Endpoint"] ?? "http://localhost:4317";
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Trinetra.API"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otelEndpoint) && Uri.TryCreate(otelEndpoint, UriKind.Absolute, out var otelUri))
        {
            tracing.AddOtlpExporter(opt => opt.Endpoint = otelUri);
        }
    });

// 3. Database Context
var connectionString = builder.Configuration.GetConnectionString("TrinetraDb");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Environment.IsEnvironment("Testing"))
    {
        options.UseInMemoryDatabase("TrinetraTestDb");
    }
    else
    {
        options.UseNpgsql(connectionString, o =>
        {
            o.UseVector();
            o.MigrationsAssembly("Trinetra.Infrastructure");
        });
    }
});

// 4. Redis Cache & Connection Multiplexer
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    try
    {
        return ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { redisConnection },
            AbortOnConnectFail = false,
            ConnectTimeout = 3000
        });
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to connect to Redis at startup. Will retry on demand.");
        return ConnectionMultiplexer.Connect(new ConfigurationOptions
        {
            EndPoints = { "localhost:6379" },
            AbortOnConnectFail = false
        });
    }
});

// 5. Auth & Core Application Services
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAuditEventWriter, DbAuditEventWriter>();
builder.Services.Configure<AiSettings>(builder.Configuration.GetSection(AiSettings.SectionName));
builder.Services.AddSingleton<IKernelFactory, SemanticKernelFactory>();
builder.Services.AddSingleton<IAgentInvoker, DefaultAgentInvoker>();
builder.Services.AddScoped<IIncidentCoordinatorStore, DbIncidentCoordinatorStore>();
builder.Services.AddScoped<CoordinatorPlugin>();
builder.Services.AddScoped<IncidentCoordinatorAgent>(sp =>
{
    var aiOptions = sp.GetRequiredService<IOptions<AiSettings>>().Value;
    var config = aiOptions.GetAgentConfig("IncidentCoordinator");
    var factory = sp.GetRequiredService<IKernelFactory>();
    var kernel = factory.CreateKernel(config);
    var logger = sp.GetRequiredService<ILogger<IncidentCoordinatorAgent>>();
    var store = sp.GetRequiredService<IIncidentCoordinatorStore>();
    var invoker = sp.GetRequiredService<IAgentInvoker>();
    var plugin = sp.GetRequiredService<CoordinatorPlugin>();
    var auditWriter = sp.GetRequiredService<IAuditEventWriter>();

    return new IncidentCoordinatorAgent(config, kernel, logger, store, invoker, plugin, auditWriter);
});

// 6. SignalR
builder.Services.AddSignalR();

// 7. JWT Authentication & RBAC Middleware
var jwtKey = builder.Configuration["Jwt:Key"] ?? "default_trinetra_super_secret_development_key_minimum_32_chars!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Trinetra.API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Trinetra.App";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // SignalR authentication via query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// 8. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// 9. Controllers & JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// 10. Health Checks
var healthChecks = builder.Services.AddHealthChecks();
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);
}

// 11. Swagger / OpenAPI Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Trinetra Autonomous IT Incident Response API",
        Version = "v1",
        Description = "Production incident investigation, diagnosis, and remediation REST API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing") || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trinetra API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors("AllowFrontend");

// Prometheus Metrics Middleware
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapHub<IncidentHub>("/hubs/incidents");
app.MapMetrics("/metrics");

// Health Check Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Expose Program for integration tests
public partial class Program { }
