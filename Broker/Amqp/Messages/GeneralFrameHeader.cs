using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public enum EFrameHeaderType
{
    METHOD = 1,
    HEADER = 2,
    BODY = 4,
    HEARTBEAT = 8,
}

public readonly struct GeneralFrameHeader
{
    public EFrameHeaderType FrameHeaderType { get; init; }
    public short Channel { get; init; }
    public int Length { get; init; }

    public static int SerializedLength = 7;

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out GeneralFrameHeader header, out int consumed)
    {
        header = default;
        consumed = 0;
        if (data.Length < SerializedLength)
        {
            return false;
        }

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryRead(out byte type);
        result &= reader.TryReadBigEndian(out short channel);
        result &= reader.TryReadBigEndian(out int length);

        if (!result)
        {
            return false;
        }

        consumed = SerializedLength;
        header = new GeneralFrameHeader()
        {
            FrameHeaderType = (EFrameHeaderType)type,
            Channel = channel,
            Length = length,
        };
        return true;
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteByte((byte)FrameHeaderType);
        writer.WriteShort(Channel);
        writer.WriteInt(Length);
    }
}
