

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text.Json;

var factory = new ConnectionFactory();
factory.UserName = "user";
factory.Password = "password";

using var connection = factory.CreateConnection();
using var model = connection.CreateModel();

model.QueueDeclare("Consumer", true, true, true);
model.QueueBind("Consumer", "amq.topic", $"events.#.heartbeat", new Dictionary<string, object>());

var consumer = new EventingBasicConsumer(model);
var msgs = 0;
var latency = 0.0;
var maxLatency = 0.0;

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

while (true)
{
    await Task.Delay(1000);
    Console.WriteLine($"maxLatency = {maxLatency} avgLatency = {latency/msgs}");
    maxLatency = 0;
    latency = 0;
    msgs = 0;
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