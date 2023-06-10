using Broker.Amqp.Extensions;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Broker.Amqp.Messages;

public readonly struct ConnectionStart : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Connection, MethodId = EMethodId.Start };

    public ConnectionStart()
    {
    }

    public byte MajorVersion { get; init; } = 0;
    public byte MinorVersion { get; init; } = 9;
    public Dictionary<string, object> ServerProperties { get; init; } = new Dictionary<string, object>() 
    {
        { "capabilities", new Dictionary<string, object>()
            {
                { "publisher_confirms", true },
                { "exchange_exchange_bindings", true },
                { "basic.nack", true },
                { "consumer_cancel_notify", true },
                { "connection_blocked", true },
                { "consumer_priorities", true },
                { "authentication_failure_close", true },
                { "per_consumer_qos", true },
                { "direct_reply_to", true },
            }
        },
        { "clustername", "name" },
        { "copyright", "none" },
        { "information", "none" },
        { "platform", RuntimeInformation.FrameworkDescription },
        { "product", "tbd" },
        { "version", "tbd" },

    };
    public string Mechanisms { get; init; } = "PLAIN AMQPPLAIN";
    public string Locales { get; init; } = "en_US";

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteByte(MajorVersion);
        writer.WriteByte(MinorVersion);
        writer.WriteDictionary(ServerProperties);
        writer.WriteLongString(Mechanisms);
        writer.WriteLongString(Locales);
    }
}
