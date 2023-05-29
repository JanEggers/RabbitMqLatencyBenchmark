

using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using MQTTnet.Packets;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

var msgs = 0;
var latency = 0.0;
var maxLatency = 0.0;

var client = ReceiveAmqp();
//await ReceiveMqtt();

while (true)
{
    await Task.Delay(1000);
    Console.WriteLine($"maxLatency = {maxLatency} avgLatency = {latency/msgs}");
    maxLatency = 0;
    latency = 0;
    msgs = 0;
}

(IConnection, IModel, EventingBasicConsumer) ReceiveAmqp() 
{
    var factory = new ConnectionFactory();
    factory.UserName = "user";
    factory.Password = "password";

    var connection = factory.CreateConnection();
    var model = connection.CreateModel();

    model.QueueDeclare("Consumer", true, true, true);
    model.QueueBind("Consumer", "amq.topic", $"events.#.heartbeat", new Dictionary<string, object>());

    var consumer = new EventingBasicConsumer(model);

    consumer.Received += OnReceived;


    void OnReceived(object? sender, BasicDeliverEventArgs e)
    {
        var heartbeat = JsonSerializer.Deserialize<Heartbeat>(e.Body.Span);
        msgs++;
        var currentlatency = (PreciseDatetime.Now - heartbeat.Timestamp).TotalMilliseconds;
        latency += currentlatency;
        maxLatency = Math.Max(maxLatency, currentlatency);
    }

    model.BasicConsume("Consumer", true, consumer);
    return (connection, model, consumer);
}

async Task ReceiveMqtt()
{
    var factory = new MqttClientAdapterFactory();
    var mqtt = new MqttClient(factory, new MqttNetNullLogger());

    await mqtt.ConnectAsync(new MqttClientOptions()
    {
        Credentials = new MqttClientCredentials("user", Encoding.UTF8.GetBytes("password")),
        ChannelOptions = new MqttClientTcpOptions()
        {
            Port = 1883,
            Server = "localhost"
        }
    });

    mqtt.ApplicationMessageReceivedAsync += async msg =>
    {
        var heartbeat = JsonSerializer.Deserialize<Heartbeat>(msg.ApplicationMessage.PayloadSegment);
        msgs++;
        var currentlatency = (PreciseDatetime.Now - heartbeat.Timestamp).TotalMilliseconds;
        latency += currentlatency;
        maxLatency = Math.Max(maxLatency, currentlatency);
    };

    await mqtt.SubscribeAsync(new MqttClientSubscribeOptions()
    {
        TopicFilters = new List<MqttTopicFilter>()
        {
            new MqttTopicFilter() 
            {
                QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce,
                Topic = "events/+/heartbeat"
            }
        }
    });
}

class Heartbeat
{
    public int Count { get; set; }

    public DateTime Timestamp { get; set; }
}

public class PreciseDatetime
{
    // using DateTime.Now resulted in many many log events with the same timestamp.
    // use static variables in case there are many instances of this class in use in the same program
    // (that way they will all be in sync)
    private static readonly Stopwatch myStopwatch = new Stopwatch();
    private static System.DateTime myStopwatchStartTime;

    static PreciseDatetime()
    {
        Reset();

        //try
        //{
        //    // In case the system clock gets updated
        //    SystemEvents.TimeChanged += SystemEvents_TimeChanged;
        //}
        //catch (Exception)
        //{
        //}
    }

    static void SystemEvents_TimeChanged(object sender, EventArgs e)
    {
        Reset();
    }

    // SystemEvents.TimeChanged can be slow to fire (3 secs), so allow forcing of reset
    public static void Reset()
    {
        myStopwatchStartTime = System.DateTime.Now;
        myStopwatch.Restart();
    }

    public static System.DateTime Now { get { return myStopwatchStartTime.Add(myStopwatch.Elapsed); } }
}