using Broker.Amqp.Messages;
using System.Collections.Immutable;

namespace Broker.Amqp.Exchanges;

public class TopicExchange : IExchange
{
    public string Name { get; set; } = "amq.topic";

    public void Deliver(CompletePublish publish, ImmutableList<AmqpConnection> connections)
    {
        foreach (var connection in connections) 
        {
            foreach (var channel in connection.Channels.Values)
            {
                foreach (var queue in channel.Queues.Values)
                {
                    foreach (var binding in queue.Bindings)
                    {
                        // todo check
                    }

                    connection.Enqueue(channel.Id, queue.Name, publish);
                }
            }
        }
    }
}
