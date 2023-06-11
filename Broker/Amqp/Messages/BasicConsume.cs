using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public class BasicConsume : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Basic, MethodId = (short)EBasicMethodId.Consume };

    public string Queue { get; init; }
    public string ConsumerTag { get; init; }
    public bool NoLocal { get; init; }
    public bool NoAck { get; init; }
    public bool Exclusive { get; init; }
    public bool Nowait { get; init; }
    public Dictionary<string, object> Arguments { get; init; }

    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public BasicConsume()
    {
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShort(0);
        writer.WriteShortString(Queue);
        writer.WriteShortString(ConsumerTag);
        writer.WriteBits(NoLocal, NoAck, Exclusive, Nowait);
        writer.WriteDictionary(Arguments);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out BasicConsume msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadBigEndian(out short _reserved1);
        result &= reader.TryReadShortString(out var queue);
        result &= reader.TryReadShortString(out var consumerTag);
        result &= reader.TryReadBits(out var noLocal, out var noAck, out var exclusive, out var nowait);
        result &= reader.TryReadDictionary(out var arguments);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new BasicConsume()
        {
            Queue = queue,
            ConsumerTag = consumerTag,
            NoLocal = noLocal,
            NoAck = noAck,
            Exclusive = exclusive,
            Nowait = nowait,
            Arguments = arguments
        };
        return true;
    }
}
