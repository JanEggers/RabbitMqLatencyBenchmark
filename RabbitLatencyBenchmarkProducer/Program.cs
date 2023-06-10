

using MQTTnet.Client;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

var mode = "Mqtt"; // Amqp

//MQTT 6000 msgs / sec max latency 20sec
//MQTT Queue V2 12000 msgs / sec max latency 18sec
var clientcount = 100;
var batchSize = 2;

// MQTT 6000 msgs/sec max latency 25ms
//var clientcount = 100;
//var batchSize = 1;

// MQTT 5000 msgs/sec max latency 30sec
//var clientcount = 50;
//var batchSize = 20;

// MQTT 6000 msgs/sec max latency 15ms
//var clientcount = 50;
//var batchSize = 2;

// MQTT 4000 msgs / sec max latency 30sec
//var clientcount = 20;
//var batchSize = 50;

// MQTT 6000 msgs / sec max latency 10ms
//var clientcount = 20;
//var batchSize = 5;

// MQTT 20000 msgs / sec max latency 50ms
//var clientcount = 12;
//var batchSize = 50;

// MQTT 30000 msgs / sec max latency 20ms
//var clientcount = 10;
//var batchSize = 100;

// MQTT 30000 msgs / sec max latency 10ms
//var clientcount = 1;
//var batchSize = 1000;

for (int i = 0; i < clientcount; i++)
{
    var plcName = $"PLC{i}";
    var j = i;

    Task.Run(() => 
    {
        if (mode == "Mqtt")
        {
            SendHeartbeatMqtt(plcName, batchSize, j);
        }
        else
        {
            SendHeartbeatAmqp(plcName, batchSize);
        }
    });
}

await Task.Delay(TimeSpan.FromDays(1));

static async Task SendHeartbeatAmqp(string plcName, int batchcount)
{
    await Task.Yield();

    var factory = new ConnectionFactory();
    factory.UserName = "user";
    factory.Password = "password";

    using var connection = factory.CreateConnection();
    using var model = connection.CreateModel();
    var prop = model.CreateBasicProperties();

    var count = 0;
    while (true)
    {
        for (int i = 0; i < batchcount; i++)
        {
            var heartbeat = new Heartbeat()
            {
                Count = ++count,
                Timestamp = PreciseDatetime.Now
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(heartbeat, typeof(Heartbeat));

            model.BasicPublish("amq.topic", $"events.{plcName}.heartbeat", prop, payload);            
        }

        await Task.Delay(10);
    }
}

static async Task SendHeartbeatMqtt(string plcName, int batchcount, int num)
{
    await Task.Yield();

    var factory = new MqttClientAdapterFactory();
    var mqtt = new MqttClient(factory, new MqttNetNullLogger());

    await mqtt.ConnectAsync(new MqttClientOptions()
    {
        Credentials = new MqttClientCredentials("user", Encoding.UTF8.GetBytes("password")),
        ChannelOptions = new MqttClientTcpOptions()
        {
            //Port = 1883 + num % 3,
            Port = 1883,
            Server = "localhost"
        }
    });

    var count = 0;
    var lastcount = 0;
    var timestamp = PreciseDatetime.Now;

    while (true)
    {
        for (int i = 0; i < batchcount; i++)
        {
            var heartbeat = new Heartbeat()
            {
                Count = ++count,
                Timestamp = PreciseDatetime.Now
            };

            var payload = JsonSerializer.SerializeToUtf8Bytes(heartbeat, typeof(Heartbeat));

            await mqtt.PublishAsync(new MQTTnet.MqttApplicationMessage()
            {
                PayloadSegment = new ArraySegment<byte>(payload, 0, payload.Length),
                Topic = $"events/{plcName}/heartbeat"
            });
        }

        await Task.Delay(10);

        if (count > lastcount + 1000)
        {
            lastcount = count;
            var now = PreciseDatetime.Now;
            var diff = now - timestamp;
            timestamp = now;

            Console.WriteLine($"published {1000} msgs for {plcName} in {diff.TotalMilliseconds}ms");
        }
    }
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