using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Trinetra.Core.Interfaces;

public class AgentTaskMessage
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public Guid IncidentId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int Priority { get; set; } = 0;
    public string? MessageId { get; set; }
}

public interface IAgentTaskQueue
{
    Task<string> EnqueueTaskAsync(AgentTaskMessage task, string? streamKey = null, CancellationToken ct = default);
}

public interface IRedisStreamConsumer
{
    Task EnsureConsumerGroupAsync(string streamKey, string consumerGroup, CancellationToken ct = default);
    Task<IReadOnlyList<AgentTaskMessage>> ReadTasksAsync(string streamKey, string consumerGroup, string consumerName, int count = 10, CancellationToken ct = default);
    Task AcknowledgeTaskAsync(string streamKey, string consumerGroup, string messageId, CancellationToken ct = default);
}
