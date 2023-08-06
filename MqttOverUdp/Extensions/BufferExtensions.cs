using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace MqttOverUdp.Extensions;

public static class IBufferWriterExtensions
{
    public static void WriteByte(this IBufferWriter<byte> writer, byte value)
    {
        writer.GetSpan(1)[0] = value;
        writer.Advance(1);
    }
    public static void WriteInt16(this IBufferWriter<byte> writer, short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }

    public static void WriteUInt16(this IBufferWriter<byte> writer, ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }

    public static void WriteInt(this IBufferWriter<byte> writer, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(writer.GetSpan(4), value);
        writer.Advance(4);
    }

    public static void WriteString(this IBufferWriter<byte> writer, string value, Encoding encoding)
    {
        writer.WriteUInt16((ushort)encoding.GetByteCount(value));
        encoding.GetBytes(value, writer);
    }

    public static void WriteLength(this IBufferWriter<byte> writer, int length) 
    {
        if (length <= 256)
        {
            writer.WriteByte((byte)length);
        }
        else
        {
            writer.WriteByte(1);
            writer.WriteUInt16((ushort)length);
        }
    }

    public static byte BuildFixedHeader(this MqttPacketType packetType, byte flags = 0)
    {
        var fixedHeader = (int)packetType << 4;
        fixedHeader |= flags;
        return (byte)fixedHeader;
    }
}