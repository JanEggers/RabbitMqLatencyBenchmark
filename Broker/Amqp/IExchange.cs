using Broker.Amqp.Messages;
using System.Collections.Immutable;

namespace Broker.Amqp;

public interface IExchange
{
    string Name { get; }

    void Deliver(CompletePublish publish, ImmutableList<AmqpConnection> connections);
}
