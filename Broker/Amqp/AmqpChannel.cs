using System.Collections.Immutable;

namespace Broker.Amqp;

public record AmqpChannel
{
    public short Id { get; set; }

    public ImmutableDictionary<string, AmqpQueue> Queues { get; set; } = ImmutableDictionary<string, AmqpQueue>.Empty;
}
