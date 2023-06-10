using Microsoft.AspNetCore.Connections;

namespace Broker.Amqp;

public class AmqpConnectionHandler : ConnectionHandler
{
    private readonly IServiceProvider _serviceProvider;

    public AmqpConnectionHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public override async Task OnConnectedAsync(ConnectionContext connection)
    {
        using var amqp = ActivatorUtilities.CreateInstance<AmqpConnection>(_serviceProvider, connection);

        await amqp.RunAsync();
    }
}
