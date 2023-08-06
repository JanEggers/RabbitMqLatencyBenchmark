namespace MqttSn;
public class Publish
{
    public string Topic { get; set; }
    public Memory<byte> Body { get; set; }
}
