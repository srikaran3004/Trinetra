using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.UnitTests.Infrastructure;

public class RedisStateStoreTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDb;
    private readonly Mock<ILogger<RedisIncidentStateStore>> _mockLogger;
    private readonly RedisIncidentStateStore _store;

    public RedisStateStoreTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisIncidentStateStore>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDb.Object);

        _store = new RedisIncidentStateStore(_mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SaveIncidentStateAsync_ShouldSetHashEntriesAndOptionalExpiry()
    {
        var incidentId = Guid.NewGuid();
        var snapshot = new IncidentStateSnapshot
        {
            IncidentId = incidentId,
            Status = "investigating",
            Severity = "P1",
            Title = "API Latency Spike",
            AssignedTo = "IncidentCoordinator",
            CurrentPhase = "Analysis",
            Metadata = new Dictionary<string, string> { { "region", "us-east-1" } }
        };

        var expectedKey = RedisIncidentStateStore.GetIncidentKey(incidentId);

        _mockDb.Setup(db => db.HashSetAsync(
            expectedKey,
            It.IsAny<HashEntry[]>(),
            It.IsAny<CommandFlags>()))
            .Returns(Task.CompletedTask);

        _mockDb.Setup(db => db.KeyExpireAsync(
            expectedKey,
            It.IsAny<TimeSpan?>(),
            It.IsAny<ExpireWhen>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await _store.SaveIncidentStateAsync(snapshot, TimeSpan.FromHours(1));

        _mockDb.Verify(db => db.HashSetAsync(
            expectedKey,
            It.Is<HashEntry[]>(entries => entries.Length == 8),
            It.IsAny<CommandFlags>()), Times.Once);

        _mockDb.Verify(db => db.KeyExpireAsync(
            expectedKey,
            TimeSpan.FromHours(1),
            ExpireWhen.Always,
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetIncidentStateAsync_WhenKeyExists_ShouldReconstituteSnapshot()
    {
        var incidentId = Guid.NewGuid();
        var expectedKey = RedisIncidentStateStore.GetIncidentKey(incidentId);

        var entries = new HashEntry[]
        {
            new("incidentId", incidentId.ToString()),
            new("status", "investigating"),
            new("severity", "P1"),
            new("title", "High Error Rate"),
            new("assignedTo", "Coordinator"),
            new("currentPhase", "Investigation"),
            new("updatedAt", DateTimeOffset.UtcNow.ToString("O")),
            new("metadata", "{\"service\":\"billing\"}")
        };

        _mockDb.Setup(db => db.HashGetAllAsync(expectedKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(entries);

        var result = await _store.GetIncidentStateAsync(incidentId);

        result.Should().NotBeNull();
        result!.IncidentId.Should().Be(incidentId);
        result.Status.Should().Be("investigating");
        result.Severity.Should().Be("P1");
        result.Title.Should().Be("High Error Rate");
        result.AssignedTo.Should().Be("Coordinator");
        result.CurrentPhase.Should().Be("Investigation");
        result.Metadata.Should().ContainKey("service");
        result.Metadata["service"].Should().Be("billing");
    }

    [Fact]
    public async Task GetIncidentStateAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        var incidentId = Guid.NewGuid();
        var expectedKey = RedisIncidentStateStore.GetIncidentKey(incidentId);

        _mockDb.Setup(db => db.HashGetAllAsync(expectedKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(Array.Empty<HashEntry>());

        var result = await _store.GetIncidentStateAsync(incidentId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetAgentStatusAsync_ShouldSerializeAndStoreInAgentsHash()
    {
        var incidentId = Guid.NewGuid();
        var agentKey = RedisIncidentStateStore.GetAgentsKey(incidentId);

        var agentState = new AgentStateSnapshot
        {
            AgentName = "log_analyzer",
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow
        };

        _mockDb.Setup(db => db.HashSetAsync(
            agentKey,
            "log_analyzer",
            It.IsAny<RedisValue>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await _store.SetAgentStatusAsync(incidentId, agentState);

        _mockDb.Verify(db => db.HashSetAsync(
            agentKey,
            "log_analyzer",
            It.Is<RedisValue>(v => v.ToString().Contains("log_analyzer") && v.ToString().Contains("running")),
            When.Always,
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetAgentStatusAsync_WhenFound_ShouldDeserialize()
    {
        var incidentId = Guid.NewGuid();
        var agentKey = RedisIncidentStateStore.GetAgentsKey(incidentId);

        var json = "{\"AgentName\":\"metrics_querier\",\"Status\":\"completed\",\"OutputSummary\":\"CPU saturation detected\"}";

        _mockDb.Setup(db => db.HashGetAsync(agentKey, "metrics_querier", It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        var result = await _store.GetAgentStatusAsync(incidentId, "metrics_querier");

        result.Should().NotBeNull();
        result!.AgentName.Should().Be("metrics_querier");
        result.Status.Should().Be("completed");
        result.OutputSummary.Should().Be("CPU saturation detected");
    }

    [Fact]
    public async Task GetAllAgentStatusesAsync_ShouldReturnAllAgents()
    {
        var incidentId = Guid.NewGuid();
        var agentKey = RedisIncidentStateStore.GetAgentsKey(incidentId);

        var entries = new HashEntry[]
        {
            new("log_analyzer", "{\"AgentName\":\"log_analyzer\",\"Status\":\"completed\"}"),
            new("metrics_querier", "{\"AgentName\":\"metrics_querier\",\"Status\":\"running\"}")
        };

        _mockDb.Setup(db => db.HashGetAllAsync(agentKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(entries);

        var result = await _store.GetAllAgentStatusesAsync(incidentId);

        result.Should().HaveCount(2);
        result.Should().ContainKey("log_analyzer");
        result.Should().ContainKey("metrics_querier");
        result["log_analyzer"].Status.Should().Be("completed");
        result["metrics_querier"].Status.Should().Be("running");
    }

    [Fact]
    public async Task DeleteIncidentStateAsync_ShouldDeleteBothKeys()
    {
        var incidentId = Guid.NewGuid();
        var stateKey = RedisIncidentStateStore.GetIncidentKey(incidentId);
        var agentsKey = RedisIncidentStateStore.GetAgentsKey(incidentId);

        _mockDb.Setup(db => db.KeyDeleteAsync(stateKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        _mockDb.Setup(db => db.KeyDeleteAsync(agentsKey, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        await _store.DeleteIncidentStateAsync(incidentId);

        _mockDb.Verify(db => db.KeyDeleteAsync(stateKey, It.IsAny<CommandFlags>()), Times.Once);
        _mockDb.Verify(db => db.KeyDeleteAsync(agentsKey, It.IsAny<CommandFlags>()), Times.Once);
    }
}
