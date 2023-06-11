using Broker.Amqp.Exchanges;
using Broker.Amqp.Messages;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Broker.Amqp;

public class AmqpBroker
{
    public ImmutableList<AmqpConnection> Connections { get; private set; } = ImmutableList<AmqpConnection>.Empty;

    public ImmutableDictionary<string, IExchange> Exchanges { get; set; } = ImmutableDictionary<string, IExchange>.Empty.Add("amq.topic", new TopicExchange());

    public Subject<IMessage> ReceivedMessages { get; } = new Subject<IMessage>();

    public AmqpBroker()
    {
        RunExchange("amq.topic");
    }

    private IDisposable RunExchange(string name) 
    {
        var exchange = Exchanges[name];
        return ReceivedMessages.OfType<CompletePublish>().Where(m => m.BasicPublish.Exchange == name).Subscribe(m => exchange.Deliver(m, Connections));
    }

    public void OnConnected(AmqpConnection connection)
    {
        Connections = Connections.Add(connection);
    }

    public void OnDisconnected(AmqpConnection connection)
    {
        Connections = Connections.Remove(connection);
    }
}
