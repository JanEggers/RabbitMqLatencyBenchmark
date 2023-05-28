using Broker.Amqp;
using Microsoft.AspNetCore.Connections;
using MQTTnet.AspNetCore;

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
    //options.ListenAnyIP(5672, listen => listen.UseConnectionHandler<AmqpConnectionHandler>());
});

var app = builder.Build();


await app.RunAsync();
