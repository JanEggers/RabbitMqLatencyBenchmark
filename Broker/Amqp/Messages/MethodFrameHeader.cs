using Broker.Amqp.Extensions;
using Broker.Amqp.Messages;
using System.Buffers;

namespace Broker.Amqp;

public enum EClassId : short
{
    None = 0,
    Connection = 10,
}

public enum EMethodId : short
{
    None = 0,
    Start = 10,
    StartOk = 11
}

public readonly struct MethodFrameHeader : IMessage
{
    public static int SerializedLength => 4;

    public EClassId ClassId { get; init; }
    public EMethodId MethodId { get; init; }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out MethodFrameHeader header, out int consumed)
    {
        header = default;
        consumed = 0;
        var reader = new SequenceReader<byte>(data);

        var result = reader.TryReadBigEndian(out short classId);
        result &= reader.TryReadBigEndian(out short methodId);
        
        if (!result)
        {
            return false;
        }

        consumed = SerializedLength;
        header = new MethodFrameHeader()
        {
            ClassId = (EClassId)classId,
            MethodId = (EMethodId)methodId,
        };

        return true;
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.WriteShort((short)ClassId);
        writer.WriteShort((short)MethodId);
    }
}
