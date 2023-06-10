using System.Buffers;

namespace Broker.Amqp.Messages;

public interface IMessage
{
    short Channel { get; }
    void Serialize(IBufferWriter<byte> writer);
}
