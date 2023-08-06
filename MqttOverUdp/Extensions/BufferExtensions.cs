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

    public static void WriteVariableByteInteger(this IBufferWriter<byte> writer, uint value)
    {
        do
        {
            var encodedByte = value % 128;
            value /= 128;
            if (value > 0)
            {
                encodedByte |= 128;
            }

            writer.WriteByte((byte)encodedByte);
        } while (value > 0);
    }

    public static byte BuildFixedHeader(this MqttPacketType packetType, byte flags = 0)
    {
        var fixedHeader = (int)packetType << 4;
        fixedHeader |= flags;
        return (byte)fixedHeader;
    }
}