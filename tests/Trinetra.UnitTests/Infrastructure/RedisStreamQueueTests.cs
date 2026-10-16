using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.Workers;
using Trinetra.Core.Entities;
using Trinetra.Core.Interfaces;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.UnitTests.Infrastructure;

public class RedisStreamQueueTests
{
    private readonly Mock<IConnectionMultiplexer> _mockRedis;
    private readonly Mock<IDatabase> _mockDb;
    private readonly Mock<ILogger<RedisStreamTaskQueue>> _mockLogger;
    private readonly RedisStreamTaskQueue _queue;

    public RedisStreamQueueTests()
    {
        _mockRedis = new Mock<IConnectionMultiplexer>();
        _mockDb = new Mock<IDatabase>();
        _mockLogger = new Mock<ILogger<RedisStreamTaskQueue>>();

        _mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(_mockDb.Object);

        _queue = new RedisStreamTaskQueue(_mockRedis.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task EnqueueTaskAsync_ShouldAddEntryToStreamAndReturnMessageId()
    {
        var task = new AgentTaskMessage
        {
            TaskId = "task-123",
            IncidentId = Guid.NewGuid(),
            AgentName = "log_analyzer",
            PayloadJson = "{\"service\":\"auth\"}",
            Priority = 1
        };

        var expectedMessageId = new RedisValue("1712345678000-0");

        _mockDb.Setup(db => db.StreamAddAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            It.IsAny<RedisValue>(),
            It.IsAny<RedisValue>(),
            It.IsAny<RedisValue?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedMessageId);

        _mockDb.Setup(db => db.StreamAddAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            It.IsAny<NameValueEntry[]>(),
            It.IsAny<RedisValue?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(expectedMessageId);

        var messageId = await _queue.EnqueueTaskAsync(task);

        messageId.Should().Be("1712345678000-0");
        task.MessageId.Should().Be("1712345678000-0");

        _mockDb.Verify(db => db.StreamAddAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            It.Is<NameValueEntry[]>(entries => entries.Any(e => e.Name == "agentName" && e.Value == "log_analyzer")),
            It.IsAny<RedisValue?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task EnsureConsumerGroupAsync_WhenGroupAlreadyExists_ShouldNotThrow()
    {
        _mockDb.Setup(db => db.StreamCreateConsumerGroupAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            RedisStreamTaskQueue.DefaultConsumerGroup,
            "0-0",
            true,
            It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisServerException("BUSYGROUP Consumer Group name already exists"));

        var act = () => _queue.EnsureConsumerGroupAsync(RedisStreamTaskQueue.DefaultStreamKey, RedisStreamTaskQueue.DefaultConsumerGroup);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReadTasksAsync_ShouldParseStreamEntriesIntoAgentTaskMessages()
    {
        var incidentId = Guid.NewGuid();
        var entries = new StreamEntry[]
        {
            new("1001-0", new NameValueEntry[]
            {
                new("taskId", "task-abc"),
                new("incidentId", incidentId.ToString()),
                new("agentName", "metrics_querier"),
                new("payload", "{\"metric\":\"cpu\"}"),
                new("createdAt", DateTimeOffset.UtcNow.ToString("O")),
                new("priority", "2")
            })
        };

        _mockDb.Setup(db => db.StreamReadGroupAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            RedisStreamTaskQueue.DefaultConsumerGroup,
            "worker-1",
            ">",
            5,
            false,
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(entries);

        var result = await _queue.ReadTasksAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            RedisStreamTaskQueue.DefaultConsumerGroup,
            "worker-1",
            5);

        result.Should().HaveCount(1);
        result[0].MessageId.Should().Be("1001-0");
        result[0].TaskId.Should().Be("task-abc");
        result[0].IncidentId.Should().Be(incidentId);
        result[0].AgentName.Should().Be("metrics_querier");
        result[0].PayloadJson.Should().Be("{\"metric\":\"cpu\"}");
        result[0].Priority.Should().Be(2);
    }

    [Fact]
    public async Task AcknowledgeTaskAsync_ShouldCallStreamAcknowledge()
    {
        _mockDb.Setup(db => db.StreamAcknowledgeAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            RedisStreamTaskQueue.DefaultConsumerGroup,
            "1001-0",
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);

        await _queue.AcknowledgeTaskAsync(RedisStreamTaskQueue.DefaultStreamKey, RedisStreamTaskQueue.DefaultConsumerGroup, "1001-0");

        _mockDb.Verify(db => db.StreamAcknowledgeAsync(
            RedisStreamTaskQueue.DefaultStreamKey,
            RedisStreamTaskQueue.DefaultConsumerGroup,
            "1001-0",
            It.IsAny<CommandFlags>()), Times.Once);
    }

    public class TestLogWorker : RedisAgentWorkerBase
    {
        public override string TargetAgentName => "log_analyzer";

        public TestLogWorker(
            IRedisStreamConsumer consumer,
            IIncidentStateStore stateStore,
            IAuditEventWriter auditWriter,
            ILogger logger)
            : base(consumer, stateStore, auditWriter, logger)
        {
        }

        protected override Task<AgentExecutionResult> ProcessTaskAsync(AgentTaskMessage task, CancellationToken ct)
        {
            return Task.FromResult(new AgentExecutionResult
            {
                Success = true,
                AgentName = task.AgentName,
                OutputSummary = "Found 3 errors in logs",
                OutputData = new { ErrorCount = 3 }
            });
        }
    }

    [Fact]
    public async Task WorkerBase_ShouldExecuteTask_UpdateState_WriteAudit_AndAcknowledge()
    {
        var mockConsumer = new Mock<IRedisStreamConsumer>();
        var mockStateStore = new Mock<IIncidentStateStore>();
        var mockAuditWriter = new Mock<IAuditEventWriter>();
        var mockLogger = new Mock<ILogger>();

        var incidentId = Guid.NewGuid();
        var task = new AgentTaskMessage
        {
            MessageId = "msg-999",
            TaskId = "task-999",
            IncidentId = incidentId,
            AgentName = "log_analyzer",
            PayloadJson = "{}"
        };

        var readCount = 0;
        mockConsumer.Setup(c => c.ReadTasksAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                readCount++;
                return readCount == 1 ? new List<AgentTaskMessage> { task } : new List<AgentTaskMessage>();
            });

        using var cts = new CancellationTokenSource();
        var worker = new TestLogWorker(mockConsumer.Object, mockStateStore.Object, mockAuditWriter.Object, mockLogger.Object);

        // Run worker and cancel quickly
        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        try { await workerTask; } catch { }

        // Verify task was acknowledged
        mockConsumer.Verify(c => c.AcknowledgeTaskAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            "msg-999",
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify state store was updated at least twice (running -> completed)
        mockStateStore.Verify(s => s.SetAgentStatusAsync(
            incidentId,
            It.Is<AgentStateSnapshot>(a => a.AgentName == "log_analyzer" && a.Status == "running"),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        mockStateStore.Verify(s => s.SetAgentStatusAsync(
            incidentId,
            It.Is<AgentStateSnapshot>(a => a.AgentName == "log_analyzer" && a.Status == "completed"),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Ground rule: audit events recorded for start and completion
        mockAuditWriter.Verify(a => a.WriteEventAsync(
            It.Is<IncidentEvent>(e => e.IncidentId == incidentId && e.AgentName == "log_analyzer" && e.EventType == "agent_task_started"),
            It.IsAny<CancellationToken>()), Times.Once);

        mockAuditWriter.Verify(a => a.WriteEventAsync(
            It.Is<IncidentEvent>(e => e.IncidentId == incidentId && e.AgentName == "log_analyzer" && e.EventType == "agent_task_completed"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
