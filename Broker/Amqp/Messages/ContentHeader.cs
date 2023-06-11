using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct ContentHeader : IMessage
{

    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.HEADER;
    public EClassId ClassId { get; init; } = EClassId.Basic;
    public short Weight { get; init; }
    public long BodySize { get; init; }
    public short PropertyFlags { get; init; }

    public ContentHeader()
    {
    }

    public ContentHeader WithChannel(short channel) 
    {
        return new ContentHeader() 
        {
            Channel = channel,
            ClassId = ClassId,
            Weight= Weight,
            BodySize = BodySize,
            PropertyFlags = PropertyFlags
        };
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteShort((short)ClassId);
        writer.WriteShort(Weight);
        writer.WriteLong(BodySize);
        writer.WriteShort(PropertyFlags);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ContentHeader msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadBigEndian(out short classId);
        result &= reader.TryReadBigEndian(out short weight);
        result &= reader.TryReadBigEndian(out long bodySize);
        result &= reader.TryReadBigEndian(out short propertyFlags);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new ContentHeader()
        {
            ClassId = (EClassId)classId,
            Weight = weight,
            BodySize = bodySize,
            PropertyFlags = propertyFlags
        };
        return true;
    }
}
