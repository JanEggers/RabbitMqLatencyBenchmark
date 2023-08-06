using System.Net.Sockets;
using System.Net;
using System.Buffers;
using System.Text;
using MqttOverUdp.Extensions;
using MqttSn;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Data;

namespace MqttOverUdp;
public class MqttSnClient
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _to = new IPEndPoint(IPAddress.Broadcast, 1883);
    private int _packetId;

    public MqttSnClient()
    {
        _udpClient = new UdpClient();
    }

    public IObservable<Message> ReceiveAsync(CancellationToken cancellationToken)
    {
        _udpClient.ExclusiveAddressUse = false;
        _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 1883));

        return Observable.Create<Message>(async observer =>
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var res = await _udpClient.ReceiveAsync(cancellationToken);
                    var msg = ReadMessage(res.Buffer);
                    if (msg != null)
                    {
                        observer.OnNext(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                observer.OnError(ex);
            }
            finally 
            {
                observer.OnCompleted();
            }

            return Disposable.Empty;
        });
    }

    public ValueTask<int> PublishAsync(Message message)
    {
        var length = 7 + message.Body.Length;

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
        
        writer.WriteUInt16((ushort)Interlocked.Increment(ref _packetId));
        writer.Write(message.Body.Span);

        return _udpClient.SendAsync(writer.WrittenMemory, _to);
    }

    private static Message? ReadMessage(ReadOnlyMemory<byte> buffer)
    {
        var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(buffer));

        var length = ReadLength(ref reader);
        var type = ReadPacketType(ref reader);

        switch (type)
        {
            case MqttPacketType.PUBLISH:
                return ReadPublish(ref reader);
            default:
                return null;
        }
    }

    private static ushort ReadLength(ref SequenceReader<byte> reader)
    {
        if (reader.UnreadSpan[0] == 1)
        {
            reader.Advance(1);
            reader.TryReadBigEndian(out short length);
            return (ushort)length;
        }
        else
        {
            var length = reader.UnreadSpan[0];
            reader.Advance(1);
            return length;
        }
    }

    private static MqttPacketType ReadPacketType(ref SequenceReader<byte> reader) 
    {
        var type = (MqttPacketType)reader.UnreadSpan[0];
        reader.Advance(1);
        return type;
    }
    private static Message ReadPublish(ref SequenceReader<byte> reader)
    {
        reader.TryRead(out var flags); 
        reader.TryReadBigEndian(out short topicid);
        reader.TryReadBigEndian(out short packetId);
        return new Message() { Body = reader.UnreadSpan.ToArray() };
    }
}
