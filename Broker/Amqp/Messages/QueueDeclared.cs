using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct QueueDeclared : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Queue, MethodId = (short)EQueueMethodId.DeclareOk };

    public string Queue { get; init; }
    public uint MessageCount { get; init; }
    public uint ConsumerCount { get; init; }
    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public QueueDeclared()
    {
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShortString(Queue);
        writer.WriteInt((int)MessageCount);
        writer.WriteInt((int)ConsumerCount);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out QueueDeclared msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadShortString(out var queue);
        result &= reader.TryReadBigEndian(out int messageCount);
        result &= reader.TryReadBigEndian(out int consumerCount);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new QueueDeclared()
        {
            Queue = queue,
            MessageCount = (uint)messageCount,
            ConsumerCount = (uint)consumerCount
        };
        return true;
    }
}
