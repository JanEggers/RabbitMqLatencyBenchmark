using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct ConnectionTuned : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Connection, MethodId = (short)EConnectionMethodId.TuneOk };

    public ConnectionTuned()
    {
    }
    public ushort MaxChannel { get; init; }
    public uint MaxFrame { get; init; }
    public ushort Heartbeat { get; init; }
    public short Channel => 0;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShort((short)MaxChannel);
        writer.WriteInt((int)MaxFrame);
        writer.WriteShort((short)Heartbeat);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ConnectionTuned msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadBigEndian(out short maxChannel);
        result &= reader.TryReadBigEndian(out int maxFrame);
        result &= reader.TryReadBigEndian(out short heartbeat);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new ConnectionTuned()
        {
            MaxChannel = (ushort)maxChannel,
            MaxFrame = (uint)maxFrame,
            Heartbeat = (ushort)heartbeat
        };
        return true;
    }
}
