using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct BasicPublish : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Basic, MethodId = (short)EBasicMethodId.Publish };
    
    public BasicPublish()
    {
    }
    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;
    public string Exchange { get; init; }
    public string RoutingKey { get; init; }
    public bool Mandatory { get; init; }
    public bool Immediate { get; init; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShort(0);
        writer.WriteShortString(Exchange);
        writer.WriteShortString(RoutingKey);
        writer.WriteBits(Mandatory, Immediate);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out BasicPublish msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadBigEndian(out short _reserved1);
        result &= reader.TryReadShortString(out var exchange);
        result &= reader.TryReadShortString(out var routingKey);
        result &= reader.TryReadBits(out var mandatory, out var immediate);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new BasicPublish()
        {
            Exchange = exchange,
            RoutingKey = routingKey,
            Mandatory = mandatory,
            Immediate = immediate
        };
        return true;
    }
}
