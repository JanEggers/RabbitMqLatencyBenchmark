namespace Broker.Amqp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAmqpBroker(this IServiceCollection services) 
    {
        return services.AddSingleton<AmqpBroker>();
    }
}
