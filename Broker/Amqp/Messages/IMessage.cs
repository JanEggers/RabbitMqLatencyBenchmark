using System.Buffers;

namespace Broker.Amqp.Messages;

public interface IMessage
{
    void Serialize(IBufferWriter<byte> writer);
}
