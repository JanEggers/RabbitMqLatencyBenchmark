using System.Net.Sockets;
using System.Net;
using System.Buffers;
using System.Text;
using MqttOverUdp.Extensions;

namespace MqttOverUdp;
public class MqttSnClient
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _to = new IPEndPoint(IPAddress.Broadcast, 1883);

    public MqttSnClient()
    {
        _udpClient = new UdpClient();
        _udpClient.ExclusiveAddressUse = false;
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 1883));
    }

    public ValueTask<int> PublishAsync(string topic, ushort packetId, Memory<byte> body)
    {
        var length = 7 + body.Length;

        if (length > 256)
        {
            length += 2;
        }

        var writer = new ArrayBufferWriter<byte>(length);

        if (length <= 256)
        {
            writer.WriteByte((byte)length);
        }
        else
        {
            writer.WriteByte(1);
            writer.WriteUInt16((ushort)length);
        }

        writer.WriteByte((byte)MqttPacketType.PUBLISH);
        writer.WriteByte((byte)0); // flags

        writer.WriteByte((byte)0); // topicid
        writer.WriteByte((byte)0); // topicid
        
        writer.WriteUInt16(packetId);
        writer.Write(body.Span);

        return _udpClient.SendAsync(writer.WrittenMemory, _to);
    }
}
