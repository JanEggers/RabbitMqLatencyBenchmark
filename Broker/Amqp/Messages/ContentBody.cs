using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct ContentBody : IMessage
{
    public byte[] Body { get; init; }

    public short Channel { get; init; }

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.BODY;

    public ContentBody WithChannel(short channel)
    {
        return new ContentBody()
        {
            Channel = channel,
            Body = Body,
        };
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ContentBody msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var body = reader.UnreadSequence.Slice(0, reader.Remaining -1).ToArray();
        reader.Advance(reader.Remaining - 1);
        var result = reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new ContentBody()
        {
            Body = body,
        };
        return true;
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        writer.Write(Body);
    }
}
