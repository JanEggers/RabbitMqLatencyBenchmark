using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct TuneConnection : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Connection, MethodId = (short)EConnectionMethodId.Tune };

    public TuneConnection()
    {
    }

    public ushort MaxChannel { get; init; } = 2047;
    public uint MaxFrame { get; init; } = 131072;
    public ushort Heartbeat { get; init; } = 60;
    public short Channel => 0;

    public EFrameHeaderType FrameHeaderType => EFrameHeaderType.METHOD;

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteShort((short)MaxChannel);
        writer.WriteInt((int)MaxFrame);
        writer.WriteShort((short)Heartbeat);
    }
}
