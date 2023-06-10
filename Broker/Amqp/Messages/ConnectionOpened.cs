using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct ConnectionOpened : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Connection, MethodId = (short)EConnectionMethodId.OpenOk };


    public ConnectionOpened()
    {
    }
    public string VirtualHost { get; init; }

    public short Channel => 0;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShortString(VirtualHost);
    }
    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ConnectionOpened msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadShortString(out var virtualHost);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new ConnectionOpened()
        {
            VirtualHost = virtualHost,
        };
        return true;
    }

}
