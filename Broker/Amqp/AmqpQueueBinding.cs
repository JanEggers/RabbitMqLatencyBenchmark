using System.Collections.Immutable;

namespace Broker.Amqp;

public record AmqpQueueBinding
{
    public string Exchange { get; init; }
    public string RoutingKey { get; init; }
    public bool Nowait { get; init; }
    public ImmutableDictionary<string, object> Arguments { get; init; }
}
