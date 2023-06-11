using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct QueueBound : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Queue, MethodId = (short)EQueueMethodId.BindOk };


    public QueueBound()
    {
    }

    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
    }
}
