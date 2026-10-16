using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Trinetra.Core.Interfaces;

namespace Trinetra.Infrastructure.Services;

public class RedisIncidentStateStore : IIncidentStateStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisIncidentStateStore> _logger;

    public RedisIncidentStateStore(IConnectionMultiplexer redis, ILogger<RedisIncidentStateStore> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private IDatabase Database => _redis.GetDatabase();

    public static string GetIncidentKey(Guid incidentId) => $"trinetra:incident:{incidentId}:state";
    public static string GetAgentsKey(Guid incidentId) => $"trinetra:incident:{incidentId}:agents";

    public async Task SaveIncidentStateAsync(IncidentStateSnapshot snapshot, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var key = GetIncidentKey(snapshot.IncidentId);
        var entries = new HashEntry[]
        {
            new("incidentId", snapshot.IncidentId.ToString()),
            new("status", snapshot.Status ?? string.Empty),
            new("severity", snapshot.Severity ?? string.Empty),
            new("title", snapshot.Title ?? string.Empty),
            new("assignedTo", snapshot.AssignedTo ?? string.Empty),
            new("currentPhase", snapshot.CurrentPhase ?? string.Empty),
            new("updatedAt", snapshot.UpdatedAt.ToString("O")),
            new("metadata", JsonSerializer.Serialize(snapshot.Metadata ?? new Dictionary<string, string>()))
        };

        try
        {
            await Database.HashSetAsync(key, entries).ConfigureAwait(false);

            if (expiry.HasValue)
            {
                await Database.KeyExpireAsync(key, expiry.Value).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save incident state for incident {IncidentId} to Redis", snapshot.IncidentId);
            throw;
        }
    }

    public async Task<IncidentStateSnapshot?> GetIncidentStateAsync(Guid incidentId, CancellationToken ct = default)
    {
        var key = GetIncidentKey(incidentId);

        try
        {
            var hash = await Database.HashGetAllAsync(key).ConfigureAwait(false);
            if (hash.Length == 0)
            {
                return null;
            }

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in hash)
            {
                dict[entry.Name.ToString()] = entry.Value.ToString();
            }

            var snapshot = new IncidentStateSnapshot
            {
                IncidentId = incidentId,
                Status = dict.GetValueOrDefault("status", string.Empty),
                Severity = dict.GetValueOrDefault("severity", string.Empty),
                Title = dict.GetValueOrDefault("title", string.Empty),
                AssignedTo = dict.TryGetValue("assignedTo", out var assigned) && !string.IsNullOrEmpty(assigned) ? assigned : null,
                CurrentPhase = dict.TryGetValue("currentPhase", out var phase) && !string.IsNullOrEmpty(phase) ? phase : null,
                UpdatedAt = dict.TryGetValue("updatedAt", out var updatedStr) && DateTimeOffset.TryParse(updatedStr, out var dto)
                    ? dto
                    : DateTimeOffset.UtcNow
            };

            if (dict.TryGetValue("metadata", out var metaJson) && !string.IsNullOrEmpty(metaJson))
            {
                try
                {
                    snapshot.Metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metaJson) ?? new();
                }
                catch
                {
                    snapshot.Metadata = new();
                }
            }

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get incident state for incident {IncidentId} from Redis", incidentId);
            throw;
        }
    }

    public async Task SetAgentStatusAsync(Guid incidentId, AgentStateSnapshot agentState, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(agentState);
        if (string.IsNullOrWhiteSpace(agentState.AgentName))
        {
            throw new ArgumentException("AgentName cannot be empty", nameof(agentState));
        }

        var key = GetAgentsKey(incidentId);
        var json = JsonSerializer.Serialize(agentState);

        try
        {
            await Database.HashSetAsync(key, agentState.AgentName, json).ConfigureAwait(false);

            if (expiry.HasValue)
            {
                await Database.KeyExpireAsync(key, expiry.Value).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set agent status for {AgentName} on incident {IncidentId} in Redis", agentState.AgentName, incidentId);
            throw;
        }
    }

    public async Task<AgentStateSnapshot?> GetAgentStatusAsync(Guid incidentId, string agentName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentName);

        var key = GetAgentsKey(incidentId);

        try
        {
            var value = await Database.HashGetAsync(key, agentName).ConfigureAwait(false);
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<AgentStateSnapshot>(value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get agent status for {AgentName} on incident {IncidentId} from Redis", agentName, incidentId);
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<string, AgentStateSnapshot>> GetAllAgentStatusesAsync(Guid incidentId, CancellationToken ct = default)
    {
        var key = GetAgentsKey(incidentId);
        var results = new Dictionary<string, AgentStateSnapshot>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var entries = await Database.HashGetAllAsync(key).ConfigureAwait(false);
            foreach (var entry in entries)
            {
                var agentName = entry.Name.ToString();
                var json = entry.Value.ToString();
                try
                {
                    var snapshot = JsonSerializer.Deserialize<AgentStateSnapshot>(json);
                    if (snapshot != null)
                    {
                        results[agentName] = snapshot;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not deserialize agent status snapshot for {AgentName}", agentName);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all agent statuses for incident {IncidentId} from Redis", incidentId);
            throw;
        }
    }

    public async Task DeleteIncidentStateAsync(Guid incidentId, CancellationToken ct = default)
    {
        var stateKey = GetIncidentKey(incidentId);
        var agentsKey = GetAgentsKey(incidentId);

        try
        {
            await Task.WhenAll(
                Database.KeyDeleteAsync(stateKey),
                Database.KeyDeleteAsync(agentsKey)
            ).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete incident state for incident {IncidentId} from Redis", incidentId);
            throw;
        }
    }
}
