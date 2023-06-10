using Broker.Amqp.Extensions;
using Broker.Amqp.Messages;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.IO.Pipelines;
using System.Reactive.Subjects;

namespace Broker.Amqp;

public class AmqpConnection : IDisposable
{
    private readonly ConnectionContext _connectionContext;
    private readonly ILogger<AmqpConnection> _logger;
    private PipeReader _pipereader;
    private PipeWriter _pipeWriter;
    private CancellationToken _cancellationToken;
    private Subject<IMessage> _receivedMessages = new();

    public AmqpConnection(ConnectionContext connectionContext, ILogger<AmqpConnection> logger)
	{
        _connectionContext = connectionContext;
        _logger = logger;
        _pipereader = connectionContext.Transport.Input;
        _pipeWriter = connectionContext.Transport.Output;
        _cancellationToken = connectionContext.ConnectionClosed;
    }

    public IObservable<IMessage> ReceivedMessages => _receivedMessages;

    public void Dispose()
    {
    }

    public async ValueTask RunAsync() 
    {
        try
        {
            await Task.Yield();
            var readResult = await _pipereader.ReadAtLeastAsync(8, _cancellationToken);
            if (!TryReadProtocolHeader(readResult))
            {
                _pipeWriter.Write(ProtocolHeader.ValidHeader);
                _pipeWriter.Advance(ProtocolHeader.ValidHeader.Length);
                await _pipeWriter.FlushAsync(_cancellationToken);
            }

            await SendAsync(new ConnectionStart());

            do
            {
                readResult = await _pipereader.ReadAtLeastAsync(7, _cancellationToken);
                if (readResult.IsCompleted || readResult.IsCanceled)
                {
                    return;
                }

                if (!TryReadMessage(readResult, out var consumed))
                {
                    _pipereader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);
                }
                else
                {
                    _pipereader.AdvanceTo(readResult.Buffer.GetPosition(consumed));
                }
            } while (!_cancellationToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private bool TryReadMessage(in ReadResult readResult, out int consumed) 
    {
        consumed = default;
        if (readResult.IsCompleted || readResult.IsCanceled)
        {
            return false;
        }
        if (!GeneralFrameHeader.TryDeserialize(readResult.Buffer, out var generalHeader, out var consumedGeneral))
        {
            return false;
        }

        if (readResult.Buffer.Length < GeneralFrameHeader.SerializedLength + generalHeader.Length + 1)
        {
            return false;
        }

        switch (generalHeader.FrameHeaderType)
        {
            case EFrameHeaderType.METHOD:
                if (!MethodFrameHeader.TryDeserialize(readResult.Buffer.Slice(consumedGeneral), out var methodHeader, out var consumedMethodHeader))
                {
                    return false;
                }

                var result = TryReadMethod(readResult.Buffer.Slice(consumedGeneral + consumedMethodHeader), methodHeader, out var consumedBody);
                consumed = consumedBody + consumedMethodHeader + consumedGeneral;
                return result;
            case EFrameHeaderType.HEADER:
                break;
            case EFrameHeaderType.BODY:
                break;
            case EFrameHeaderType.HEARTBEAT:
                break;
            default:
                throw new NotSupportedException(generalHeader.FrameHeaderType.ToString());
        }
        return false;
    }

    private bool TryReadMethod(in ReadOnlySequence<byte> data, in MethodFrameHeader header, out int consumed)
    {
        switch ((header.ClassId, header.MethodId))
        {
            case (EClassId.Connection, EMethodId.StartOk):
                if (ConnectionStartOk.TryDeserialize(data, out var msg, out consumed)) 
                {
                    _receivedMessages.OnNext(msg);
                    return true;
                }
                break;
            default:
                throw new NotSupportedException(header.ClassId.ToString() + header.MethodId.ToString());
        }
        return false;
    }

    private bool TryReadProtocolHeader(in ReadResult readResult) 
    {
        if (readResult.IsCompleted || readResult.IsCanceled)
        {
            return false;
        }

        if (!ProtocolHeader.TryDeserialize(readResult.Buffer, out var header, out var consumed))
        {
            return false;
        }

        _pipereader.AdvanceTo(readResult.Buffer.GetPosition(consumed));
        return true;
    }

    private async ValueTask SendAsync<T>(T message) where T : IMessage
    {
        var writer = new ArrayBufferWriter<byte>();
        message.Serialize(writer);
        var writer2 = new ArrayBufferWriter<byte>();
        new GeneralFrameHeader() { FrameHeaderType = EFrameHeaderType.METHOD, Length = writer.WrittenCount }.Serialize(_pipeWriter);
        _pipeWriter.Write(writer.WrittenSpan);
        _pipeWriter.WriteByte(0xce);
        await _pipeWriter.FlushAsync(_cancellationToken);
    }
}
