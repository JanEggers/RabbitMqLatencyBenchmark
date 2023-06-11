using Microsoft.AspNetCore.Connections;

namespace Broker.Amqp;

public class AmqpConnectionHandler : ConnectionHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AmqpBroker _broker;

    public AmqpConnectionHandler(IServiceProvider serviceProvider, AmqpBroker broker)
    {
        _serviceProvider = serviceProvider;
        _broker = broker;
    }
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        using var amqp = ActivatorUtilities.CreateInstance<AmqpConnection>(_serviceProvider, connection);

        try
        {
            _broker.OnConnected(amqp);

            using var sub = amqp.ReceivedMessages.Subscribe(_broker.ReceivedMessages.OnNext);

            await amqp.RunAsync();
        }
        finally
        {
            _broker.OnDisconnected(amqp);
        }
    }
}
