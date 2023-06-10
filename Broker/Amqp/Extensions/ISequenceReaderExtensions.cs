using System.Buffers;
using System.Text;

namespace Broker.Amqp.Extensions;

public static class ISequenceReaderExtensions
{
    public static bool TryReadDictionary(this ref SequenceReader<byte> reader, out Dictionary<string, object>? value)
    {
        value = default;
        if (!reader.TryReadBigEndian(out int bufferlength)) 
        {
            return false;
        }

        if (reader.Remaining < bufferlength)
        {
            return false;
        }

        var nestedReader = new SequenceReader<byte>(reader.UnreadSequence.Slice(0, bufferlength));

        value = new Dictionary<string, object>();
        while (nestedReader.Consumed < bufferlength)
        {
            var result = nestedReader.TryReadShortString(out var key);
            result &= nestedReader.TryReadObject(out var obj);
            if (!result)
            {
                return false;
            }

            value[key] = obj;
        }

        reader.Advance(nestedReader.Consumed);
        return true;
    }


    public static bool TryReadShortString(this ref SequenceReader<byte> reader, out string value)
    {
        value = default;
        if (!reader.TryRead(out var count))
        {
            return false;
        }


        var data = reader.UnreadSequence.Slice(0, count);
        if (data.IsSingleSegment)
        {
            value = Encoding.UTF8.GetString(data.FirstSpan);
            reader.Advance(count);
            return true;
        }

        Span<byte> buffer = stackalloc byte[count];
        data.CopyTo(buffer);
        value = Encoding.UTF8.GetString(buffer);
        reader.Advance(count);
        return true;
    }

    public static bool TryReadLongString(this ref SequenceReader<byte> reader, out string value)
    {
        value = default;
        if (!reader.TryReadBigEndian(out int count))
        {
            return false;
        }


        var data = reader.UnreadSequence.Slice(0, count);
        if (data.IsSingleSegment)
        {
            value = Encoding.UTF8.GetString(data.FirstSpan);
            reader.Advance(count);
            return true;
        }

        var buffer = ArrayPool<byte>.Shared.Rent(count);
        try
        {
            data.CopyTo(buffer.AsSpan(0, count));
            value = Encoding.UTF8.GetString(buffer.AsSpan(0, count));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
        reader.Advance(count);
        return true;
    }
    public static bool TryReadObject(this ref SequenceReader<byte> reader, out object value)
    {
        value = default;
        if (!reader.TryRead(out var type))
        {
            return false;
        }

        switch ((char)type)
        {
            case 'S':
                {
                    var result = reader.TryReadLongString(out var s);
                    value = s;
                    return result;
                }
            case 't':
                {
                    var result = reader.TryRead(out var t);
                    value = t == 1;
                    return result;
                }
            //case int I:
            //    writer.WriteChar('I');
            //    writer.WriteInt(I);
            //    break;
            case 'V':
                value = null;
                return true;
            case 'F':
                {
                    var result = reader.TryReadDictionary(out var s);
                    value = s;
                    return result;
                }
            //case IList list:
            //    writer.WriteChar('A');
            //    writer.WriteList(list);
            //    break;
            //case long l:
            //    writer.WriteChar('l');
            //    writer.WriteLong(l);
            //    break;
            //case uint i:
            //    writer.WriteChar('i');
            //    writer.WriteUInt(i);
            //    break;
            //case decimal D:
            //    writer.WriteChar('D');
            //    writer.WriteDecimal(D);
            //    break;
            //case byte B:
            //    writer.WriteChar('B');
            //    writer.WriteByte(B);
            //    break;
            //case sbyte b:
            //    writer.WriteChar('b');
            //    writer.WriteByte((byte)b);
            //    break;
            //case double d:
            //    writer.WriteChar('d');
            //    writer.WriteDouble(d);
            //    break;
            //case float f:
            //    writer.WriteChar('f');
            //    writer.WriteFloat(f);
            //    break;
            //case short s:
            //    writer.WriteChar('s');
            //    writer.WriteShort(s);
            //    break;
            //case ushort u:
            //    writer.WriteChar('u');
            //    writer.WriteShort((short)u);
            //    break;
            //case 'T':
            //    writer.WriteChar('T');
            //    bytesRead = 1 + ReadTimestamp(slice, out var timestamp);
            //    return timestamp;
            //case 'x':
            //    writer.WriteChar('x');
            //    bytesRead = 1 + ReadLongstr(slice, out var binaryTableResult);
            //    return new BinaryTableValue(binaryTableResult);
            default:
                throw new NotSupportedException((char)type + " not supported");
        }


        return true;
    }
}
