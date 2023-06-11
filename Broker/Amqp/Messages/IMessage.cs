using System.Buffers;

namespace Broker.Amqp.Messages;

public interface IMessage
{
    EFrameHeaderType FrameHeaderType { get; }
    short Channel { get; }
    void Serialize(IBufferWriter<byte> writer);
}
