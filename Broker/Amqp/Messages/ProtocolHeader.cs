using System.Buffers;

namespace Broker.Amqp;

public readonly struct ProtocolHeader
{

    public static byte[] ValidHeader { get; } = new byte[] { (byte)'A', (byte)'M', (byte)'Q', (byte)'P', 0, 0, 9, 1 };
    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ProtocolHeader header, out int consumed) 
    {
        header = default;
        consumed = default;
        if (data.Length < ValidHeader.Length)
        {
            return false;
        }

        if (!data.FirstSpan.Slice(0, ValidHeader.Length).SequenceEqual(ValidHeader))
        {
            return false;
        }
        consumed = ValidHeader.Length;
        return true;
    }
}
