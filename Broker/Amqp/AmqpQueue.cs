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

    public BasicConsume Consumer { get; init; }
}
