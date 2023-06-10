using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct BindQueue : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Queue, MethodId = (short)EQueueMethodId.Bind };

    public short Channel { get; init; }

    public string Queue { get; init; }
    public string Exchange { get; init; }
    public string RoutingKey { get; init; }
    public bool Nowait { get; init; }
    public Dictionary<string, object> Arguments { get; init; }

    public BindQueue()
    {
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShort(0);
        writer.WriteShortString(Queue);
        writer.WriteShortString(Exchange);
        writer.WriteShortString(RoutingKey);
        writer.WriteBits(Nowait);
        writer.WriteDictionary(Arguments);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out BindQueue msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadBigEndian(out short reserved);
        result &= reader.TryReadShortString(out var queue);
        result &= reader.TryReadShortString(out var exchange);
        result &= reader.TryReadShortString(out var routingKey);
        result &= reader.TryReadBits(out var nowait);
        result &= reader.TryReadDictionary(out var arguments);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new BindQueue()
        {
            Queue = queue,
            Exchange = exchange,
            RoutingKey = routingKey,
            Nowait = nowait,
            Arguments = arguments
        };
        return true;
    }
}
