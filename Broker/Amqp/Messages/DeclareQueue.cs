using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct DeclareQueue : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Queue, MethodId = (short)EQueueMethodId.Declare };

    public short Channel { get; init; }


    public string Queue { get; init; }
    public bool Passive { get; init; }
    public bool Durable { get; init; }
    public bool Exclusive { get; init; }
    public bool AutoDelete { get; init; }
    public bool Nowait { get; init; }
    public Dictionary<string, object> Arguments { get; init; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out DeclareQueue msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadBigEndian(out short reserved);
        result &= reader.TryReadShortString(out var queue);
        result &= reader.TryReadBits(out var passive, out var durable, out var exclusive, out var autoDelete, out var nowait);
        result &= reader.TryReadDictionary(out var arguments);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new DeclareQueue()
        {
            Queue = queue,
            Passive = passive,
            Durable = durable,
            Exclusive = exclusive,
            AutoDelete = autoDelete,
            Nowait = nowait,
            Arguments = arguments,
        };
        return true;
    }
}
