using Broker.Amqp;
using Broker.Amqp.Extensions;
using Broker.Amqp.Messages;
using Microsoft.AspNetCore.Connections;
using MQTTnet.AspNetCore;
using MQTTnet.Server;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedMqttServer(optionsBuilder =>
{
    optionsBuilder.WithDefaultEndpoint();
});

builder.Services.AddMqttConnectionHandler();
builder.Services.AddConnections();
builder.Services.AddAmqpBroker();

builder.WebHost.ConfigureKestrel(options => 
{
    options.ListenAnyIP(1883, listen => listen.UseMqtt());
    options.ListenAnyIP(5672, listen => listen.UseAmqp());
});

var app = builder.Build();

var mqttServer = app.Services.GetRequiredService<MqttServer>();
var amqpBroker = app.Services.GetRequiredService<AmqpBroker>();

var runServer = app.RunAsync();

//var factory = new ConnectionFactory();
//factory.UserName = "user";
//factory.Password = "password";

//using var connection = factory.CreateConnection();
//using var model = connection.CreateModel();
//var prop = model.CreateBasicProperties();

mqttServer.InterceptingPublishAsync += OnPublish;

//Task OnPublish(InterceptingPublishEventArgs arg)
//{
//    model.BasicPublish("amq.topic", arg.ApplicationMessage.Topic.Replace("/", "."), prop, arg.ApplicationMessage.PayloadSegment);
//    return Task.CompletedTask;
//}

Task OnPublish(InterceptingPublishEventArgs arg)
{
    amqpBroker.ReceivedMessages.OnNext(new CompletePublish()
    {
        BasicPublish = new BasicPublish() 
        {
            Exchange = "amq.topic",
            RoutingKey = arg.ApplicationMessage.Topic.Replace("/", "."),            
        },
        Header = new ContentHeader() 
        { 
            BodySize = arg.ApplicationMessage.PayloadSegment.Count
        },
        Body = new ContentBody() 
        {
            Body = arg.ApplicationMessage.PayloadSegment.ToArray()
        }
    });
    return Task.CompletedTask;
}

await runServer;
