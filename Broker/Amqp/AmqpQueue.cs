using Broker.Amqp.Messages;
using System.Collections.Immutable;

namespace Broker.Amqp;

public record AmqpQueue 
{
    public string Name { get; init; }
    public bool Passive { get; init; }
    public bool Durable { get; init; }
    public bool Exclusive { get; init; }
    public bool AutoDelete { get; init; }
    public bool Nowait { get; init; }
    public ImmutableDictionary<string, object> Arguments { get; init; } = ImmutableDictionary<string, object>.Empty;

    public ImmutableList<AmqpQueueBinding> Bindings { get; init; } = ImmutableList<AmqpQueueBinding>.Empty;

    public ImmutableQueue<CompletePublish> Items { get; init; } = ImmutableQueue<CompletePublish>.Empty;

    public ulong? PendingAck { get; init; }


    public BasicConsume Consumer { get; init; }

    public ManualResetEvent QueueEmptyWait { get; init; } = new ManualResetEvent(false);
}
