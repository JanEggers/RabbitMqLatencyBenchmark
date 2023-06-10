﻿using Broker.Amqp.Extensions;
using System.Buffers;

namespace Broker.Amqp.Messages;

public readonly struct OpenChannel : IMessage
{
    public static MethodFrameHeader Header = new MethodFrameHeader() { ClassId = EClassId.Channel, MethodId = (short)EConnectionMethodId.Open };

    public static OpenChannel Instance = new OpenChannel();
    public short Channel => 0;
    public OpenChannel()
    {
    }

    public void Serialize(IBufferWriter<byte> writer)
    {
        Header.Serialize(writer);
        writer.WriteByte(0);
    }

    public static bool TryDeserialize(in ReadOnlySequence<byte> data, out OpenChannel msg, out int consumed)
    {
        msg = default;
        consumed = 0;

        var reader = new SequenceReader<byte>(data);
        var result = reader.TryRead(out var _reserved1);
        result &= reader.TryRead(out var end) && end == 0xce;

        if (!result)
        {
            return false;
        }

        consumed = (int)reader.Consumed;
        msg = Instance;
        return true;
    }
}