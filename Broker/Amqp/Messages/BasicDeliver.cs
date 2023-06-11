using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct BasicDeliver : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Basic, MethodId = (short)EBasicMethodId.Deliver };

    public BasicDeliver()
    {
    }

    public string ConsumerTag { get; init; }
    public ulong DeliveryTag { get; init; }
    public string Exchange { get; init; }
    public string RoutingKey { get; init; }

    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShortString(ConsumerTag);
        writer.WriteULong(DeliveryTag);
        writer.WriteBits(); // reserved
        writer.WriteShortString(Exchange);
        writer.WriteShortString(RoutingKey);
    }
}
