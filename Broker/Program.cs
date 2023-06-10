using Broker.Amqp;
using Broker.Amqp.Extensions;
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

builder.WebHost.ConfigureKestrel(options => 
{
    options.ListenAnyIP(1883, listen => listen.UseMqtt());
    options.ListenAnyIP(5672, listen => listen.UseAmqp());
});

var app = builder.Build();

var mqttServer = app.Services.GetRequiredService<MqttServer>();

app.RunAsync();

var factory = new ConnectionFactory();
factory.UserName = "user";
factory.Password = "password";

using var connection = factory.CreateConnection();
using var model = connection.CreateModel();
var prop = model.CreateBasicProperties();

mqttServer.InterceptingPublishAsync += OnPublish;
Task OnPublish(InterceptingPublishEventArgs arg)
{
    model.BasicPublish("amq.topic", arg.ApplicationMessage.Topic.Replace("/", "."), prop, arg.ApplicationMessage.PayloadSegment);
    return Task.CompletedTask;
}
