using RabbitMQ.Client;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections;
using System.Drawing;
using System.Text;

namespace Broker.Amqp.Extensions;

public static class IBufferWriterExtensions
{
    public static void WriteByte(this IBufferWriter<byte> writer, byte value)
    {
        writer.GetSpan(1)[0] = value;
        writer.Advance(1);
    }
    public static void WriteChar(this IBufferWriter<byte> writer, char value)
    {
        writer.GetSpan(1)[0] = (byte)value;
        writer.Advance(1);
    }

    public static void WriteBool(this IBufferWriter<byte> writer, bool value)
    {
        writer.GetSpan(1)[0] = (byte)(value ? 1 : 0);
        writer.Advance(1);
    }
    public static void WriteShort(this IBufferWriter<byte> writer, short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(writer.GetSpan(2), value);
        writer.Advance(2);
    }
    public static void WriteInt(this IBufferWriter<byte> writer, int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(writer.GetSpan(4), value);
        writer.Advance(4);
    }
    public static void WriteLongString(this IBufferWriter<byte> writer, string value)
    {
        writer.WriteInt(Encoding.UTF8.GetByteCount(value));
        Encoding.UTF8.GetBytes(value, writer);
    }

    public static void WriteShortString(this IBufferWriter<byte> writer, string value)
    {
        writer.WriteByte((byte)Encoding.UTF8.GetByteCount(value));
        Encoding.UTF8.GetBytes(value, writer);
    }

    public static void WriteBits(this IBufferWriter<byte> writer, bool val1, bool val2, bool val3, bool val4, bool val5)
    {
        byte destination = 0;

        destination |= (byte)(val1 ? 1 : 0);
        destination |= (byte)(val2 ? 2 : 0);
        destination |= (byte)(val3 ? 4 : 0);
        destination |= (byte)(val4 ? 8 : 0);
        destination |= (byte)(val5 ? 16 : 0);

        writer.WriteByte(destination);
    }
    public static void WriteDictionary(this IBufferWriter<byte> writer, Dictionary<string, object>? value)
    {
        if (value == null) 
        {
            writer.WriteInt(0);
            return;
        }
        var temp = new ArrayBufferWriter<byte>();

        foreach (var kvp in value) 
        {
            WriteShortString(temp, kvp.Key);
            WriteObject(temp, kvp.Value);
        }

        writer.WriteInt(temp.WrittenCount);
        writer.Write(temp.WrittenSpan);
    }

    public static void WriteList(this IBufferWriter<byte> writer, IList value) 
    {

    }
    public static void WriteLong(this IBufferWriter<byte> writer, long value)
    {

    }
    public static void WriteUInt(this IBufferWriter<byte> writer, uint value)
    {

    }
    public static void WriteDecimal(this IBufferWriter<byte> writer, decimal value)
    {

    }
    public static void WriteDouble(this IBufferWriter<byte> writer, double value)
    {

    }
    public static void WriteFloat(this IBufferWriter<byte> writer, float value)
    {

    }

    public static void WriteObject(this IBufferWriter<byte> writer, object value)
    {
        switch (value)
        {
            case string s:
                writer.WriteChar('S');
                writer.WriteLongString(s);
                break;
            case bool t:
                writer.WriteChar('t');
                writer.WriteBool(t);
                break;
            case int I:
                writer.WriteChar('I');
                writer.WriteInt(I);
                break;
            case null:
                writer.WriteChar('V');
                break;
            case Dictionary<string, object> dict:
                writer.WriteChar('F');
                writer.WriteDictionary(dict);
                break;
            case IList list:
                writer.WriteChar('A');
                writer.WriteList(list);
                break;
            case long l:
                writer.WriteChar('l');
                writer.WriteLong(l);
                break;
            case uint i:
                writer.WriteChar('i');
                writer.WriteUInt(i);
                break;
            case decimal D:
                writer.WriteChar('D');
                writer.WriteDecimal(D);
                break;
            case byte B:
                writer.WriteChar('B');
                writer.WriteByte(B);
                break;
            case sbyte b:
                writer.WriteChar('b');
                writer.WriteByte((byte)b);
                break;
            case double d:
                writer.WriteChar('d');
                writer.WriteDouble(d);
                break;
            case float f:
                writer.WriteChar('f');
                writer.WriteFloat(f);
                break;
            case short s:
                writer.WriteChar('s');
                writer.WriteShort(s);
                break;
            case ushort u:
                writer.WriteChar('u');
                writer.WriteShort((short)u);
                break;
            //case 'T':
            //    writer.WriteChar('T');
            //    bytesRead = 1 + ReadTimestamp(slice, out var timestamp);
            //    return timestamp;
            //case 'x':
            //    writer.WriteChar('x');
            //    bytesRead = 1 + ReadLongstr(slice, out var binaryTableResult);
            //    return new BinaryTableValue(binaryTableResult);
            default:
                throw new NotSupportedException(value.GetType().FullName);
        }
    }
}
