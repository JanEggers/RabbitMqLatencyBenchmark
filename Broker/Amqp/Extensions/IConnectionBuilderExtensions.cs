using Microsoft.AspNetCore.Connections;

namespace Broker.Amqp.Extensions;

public static class IConnectionBuilderExtensions
{
    public static IConnectionBuilder UseAmqp(this IConnectionBuilder connectionBuilder) 
    {
        return connectionBuilder.UseConnectionHandler<AmqpConnectionHandler>();
    }
}
