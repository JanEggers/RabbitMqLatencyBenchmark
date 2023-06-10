using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct Heartbeat : IMessage
{
    public static GeneralFrameHeader Header = new GeneralFrameHeader() { FrameHeaderType = EFrameHeaderType.HEARTBEAT };

    public static Heartbeat Instance = new Heartbeat();
    public short Channel => 0;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out Heartbeat msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = Instance;
        return true;
    }
}
