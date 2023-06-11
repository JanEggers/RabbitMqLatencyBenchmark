using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct ChannelOpened : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Channel, MethodId = (short)EChannelMethodId.OpenOk };

    public static ChannelOpened Instance = new ChannelOpened();
    public ChannelOpened()
    {
    }
    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteInt(0);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ChannelOpened msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryRead(out var _reserved1);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = Instance;
        return true;
    }
}
