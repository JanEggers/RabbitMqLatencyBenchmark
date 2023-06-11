using System.Buffers;

namespace Broker.Amqp.Messages;

public class CompletePublish : IMessage
{
    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public BasicPublish BasicPublish { get; init; }

    public ContentHeader Header { get; init; }

    public ContentBody Body { get; init; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        throw new NotImplementedException();
    }
}
