using Broker.Amqp.Messages;
using System.Collections.Immutable;

namespace Broker.Amqp;

public record AmqpChannel
{
    public short Id { get; init; }

    public ImmutableDictionary<string, AmqpQueue> Queues { get; init; } = ImmutableDictionary<string, AmqpQueue>.Empty;

    public BasicPublish? CurrentBasicPublish { get; init; }

    public ContentHeader? CurrentContentHeader { get; init; }

    public AmqpChannel UpdateQueue(string name, Func<AmqpQueue, AmqpQueue> update)
    {
        return this with
        {
            Queues = Queues.SetItem(name, update(Queues[name]))
        };
    }
}
