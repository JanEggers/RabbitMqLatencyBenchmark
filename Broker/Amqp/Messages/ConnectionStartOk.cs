using Broker.Amqp.Extensions;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Broker.Amqp.Messages;

public readonly struct ConnectionStartOk : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Connection, MethodId = EMethodId.StartOk };

    public ConnectionStartOk()
    {
    }

    public Dictionary<string, object>? ClientProperties { get; init; }
    public string Mechanism { get; init; }
    public string Response { get; init; }
    public string Locale { get; init; }

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteDictionary(ClientProperties);
        writer.WriteShortString(Mechanism);
        writer.WriteLongString(Response);
        writer.WriteShortString(Locale);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out ConnectionStartOk msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryReadDictionary(out var clientProperties);
        result &= reader.TryReadShortString(out var mechanism);
        result &= reader.TryReadLongString(out var response);
        result &= reader.TryReadShortString(out var locale);

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = new ConnectionStartOk()
        {
            ClientProperties = clientProperties,
            Mechanism = mechanism,
            Response = response,
            Locale = locale
        };
        return true;
    }
}
