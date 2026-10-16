using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Trinetra.Agents;
using Trinetra.Agents.Configuration;
using Trinetra.Agents.Orchestration;
using Trinetra.Agents.Workers;
using Trinetra.Core.Interfaces;
using Trinetra.Core.Tracing;
using Trinetra.Infrastructure.Services;
using Xunit;

namespace Trinetra.IntegrationTests;

public class OpenTelemetryAgentTracingTests
{
    private class TracedTestWorker : RedisAgentWorkerBase
    {
        public override string TargetAgentName => "metrics_querier";

        public TracedTestWorker(
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
                OutputSummary = "Traced analysis complete",
                ExecutionTimeMs = 42
            });
        }
    }

    private class TracedAgent : AgentBase
    {
        public TracedAgent(IAuditEventWriter auditWriter, ILogger logger)
            : base(
                "diagnostics_agent",
                new AgentConfiguration { ModelId = "claude-sonnet-4-5" },
                new Microsoft.SemanticKernel.Kernel(),
                logger,
                auditWriter)
        {
        }
    }

    [Fact]
    public async Task AgentInvoker_CreatesActivityWithCorrectTagsAndStatus()
    {
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == TrinetraActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var mockLogger = new Mock<ILogger<DefaultAgentInvoker>>();
        var invoker = new DefaultAgentInvoker(mockLogger.Object);
        var incidentId = Guid.NewGuid();

        var request = new AgentInvocationRequest
        {
            AgentName = "log_analyzer",
            Action = "extract_stack_traces",
            IncidentId = incidentId
        };

        var result = await invoker.InvokeAgentAsync(request);

        result.Success.Should().BeTrue();
        capturedActivities.Should().Contain(a =>
            a.OperationName == "AgentInvoke log_analyzer:extract_stack_traces" &&
            a.GetTagItem("trinetra.agent.name")!.ToString() == "log_analyzer" &&
            a.GetTagItem("trinetra.incident.id")!.ToString() == incidentId.ToString() &&
            a.Status == ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task AgentBase_StartAgentActivity_And_RecordAuditEvent_CreatesSpansAndEvents()
    {
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == TrinetraActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var mockAuditWriter = new Mock<IAuditEventWriter>();
        mockAuditWriter.Setup(a => a.WriteEventAsync(It.IsAny<Trinetra.Core.Entities.IncidentEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Trinetra.Core.Entities.IncidentEvent e, CancellationToken _) => e);

        var mockLogger = new Mock<ILogger>();
        var agent = new TracedAgent(mockAuditWriter.Object, mockLogger.Object);
        var incidentId = Guid.NewGuid();

        using (var activity = agent.StartAgentActivity("analyze_anomalies"))
        {
            activity.Should().NotBeNull();
            await agent.RecordAuditEventAsync(
                incidentId,
                "anomaly_detected",
                "Spike in 500 error responses",
                toolCall: "detect_anomalies",
                guardrailTriggered: true,
                guardrailName: "ReadOnlyGuardrail");
        }

        capturedActivities.Should().Contain(a =>
            a.OperationName == "diagnostics_agent analyze_anomalies" &&
            a.GetTagItem("trinetra.agent.name")!.ToString() == "diagnostics_agent" &&
            (bool)a.GetTagItem("trinetra.guardrail.triggered")! == true &&
            a.GetTagItem("trinetra.guardrail.name")!.ToString() == "ReadOnlyGuardrail");
    }

    [Fact]
    public async Task RedisStream_TraceContextPropagation_PreservesTraceParentAcrossAsyncBoundaries()
    {
        var capturedActivities = new List<Activity>();

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == TrinetraActivitySource.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var mockRedis = new Mock<IConnectionMultiplexer>();
        var mockDb = new Mock<IDatabase>();
        mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);

        mockDb.Setup(db => db.StreamAddAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<NameValueEntry[]>(),
            It.IsAny<RedisValue?>(),
            It.IsAny<int?>(),
            It.IsAny<bool>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("1700000000-0"));

        var queueLogger = new Mock<ILogger<RedisStreamTaskQueue>>();
        var queue = new RedisStreamTaskQueue(mockRedis.Object, queueLogger.Object);

        var incidentId = Guid.NewGuid();
        var task = new AgentTaskMessage
        {
            TaskId = "trace-task-1",
            IncidentId = incidentId,
            AgentName = "metrics_querier"
        };

        string parentTraceId;
        using (var parentActivity = TrinetraActivitySource.Source.StartActivity("RootIncidentInvestigation"))
        {
            parentActivity.Should().NotBeNull();
            parentTraceId = parentActivity!.Id!;

            // Enqueue task within parent activity span
            await queue.EnqueueTaskAsync(task);
        }

        // Verify task has traceparent captured from active parent activity
        task.TraceParent.Should().NotBeNullOrEmpty();
        task.TraceParent.Should().Be(parentTraceId);

        // Now simulate consumer reading and executing task on background worker
        var mockConsumer = new Mock<IRedisStreamConsumer>();
        var mockStateStore = new Mock<IIncidentStateStore>();
        var mockAuditWriter = new Mock<IAuditEventWriter>();
        var mockWorkerLogger = new Mock<ILogger>();

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
        var worker = new TracedTestWorker(mockConsumer.Object, mockStateStore.Object, mockAuditWriter.Object, mockWorkerLogger.Object);

        var workerTask = worker.StartAsync(cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        try { await workerTask; } catch { }

        // Verify child consumer activity was created with parent context preserved
        capturedActivities.Should().Contain(a =>
            a.OperationName == "AgentWorker metrics_querier" &&
            a.ParentId == parentTraceId &&
            a.GetTagItem("trinetra.agent.name")!.ToString() == "metrics_querier" &&
            a.Status == ActivityStatusCode.Ok);
    }
}
