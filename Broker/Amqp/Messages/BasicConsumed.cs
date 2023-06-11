using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct BasicConsumed : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Basic, MethodId = (short)EBasicMethodId.ConsumeOk };

    public string ConsumerTag { get; init; }
    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShortString(ConsumerTag);
    }
}
