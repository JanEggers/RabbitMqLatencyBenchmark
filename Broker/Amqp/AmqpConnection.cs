using Broker.Amqp.Extensions;
using Broker.Amqp.Messages;
using Microsoft.AspNetCore.Connections;
using System.Buffers;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Reactive.Subjects;
using System.Threading.Channels;

namespace Broker.Amqp;

public class AmqpConnection : IDisposable
{
    private readonly ConnectionContext _connectionContext;
    private readonly ILogger<AmqpConnection> _logger;
    private PipeReader _pipereader;
    private PipeWriter _pipeWriter;
    private CancellationToken _cancellationToken;
    private Subject<IMessage> _receivedMessages = new();
    private Channel<IMessage> _writeBuffer = Channel.CreateBounded<IMessage>(1024);
    private ImmutableDictionary<short, AmqpChannel> _channels = ImmutableDictionary<short, AmqpChannel>.Empty;

    public string? VirtualHost { get; private set; }

    public ImmutableDictionary<short, AmqpChannel> Channels => _channels;
    public IObservable<IMessage> ReceivedMessages => _receivedMessages;

    public AmqpConnection(ConnectionContext connectionContext, ILogger<AmqpConnection> logger)
	{
        _connectionContext = connectionContext;
        _logger = logger;
        _pipereader = connectionContext.Transport.Input;
        _pipeWriter = connectionContext.Transport.Output;
        _cancellationToken = connectionContext.ConnectionClosed;
    }

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

            await SendAsync(new StartConnection());

            var readLoop = RunReadLoop();
            var writeLoop = RunWriteLoop();

            await await Task.WhenAny(readLoop, writeLoop);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    private async Task RunWriteLoop()
    {
        while (!_cancellationToken.IsCancellationRequested) 
        {
            while (_writeBuffer.Reader.TryRead(out var msg))
            {
                Write(msg);
            }
            await _pipeWriter.FlushAsync();

            await _writeBuffer.Reader.WaitToReadAsync(_cancellationToken);
        }
    }

    private async Task RunReadLoop()
    {
        await Task.Yield();
        do
        {
            var readResult = await _pipereader.ReadAtLeastAsync(7, _cancellationToken);
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

                var result = TryReadClassMethod(readResult.Buffer.Slice(consumedGeneral + consumedMethodHeader), generalHeader, methodHeader, out var consumedBody);
                consumed = consumedBody + consumedMethodHeader + consumedGeneral;
                return result;
            case EFrameHeaderType.HEADER:
                break;
            case EFrameHeaderType.BODY:
                break;
            case EFrameHeaderType.HEARTBEAT:
                if (!Heartbeat.TryDeserialize(readResult.Buffer.Slice(consumedGeneral), out var heartbeat, out var consumedHeartbeat))
                {
                    return false;
                }
                _receivedMessages.OnNext(heartbeat);
                consumed = consumedHeartbeat + consumedGeneral;
                break;
            default:
                throw new NotSupportedException(generalHeader.FrameHeaderType.ToString());
        }
        return false;
    }

    private bool TryReadClassMethod(in ReadOnlySequence<byte> data, in GeneralFrameHeader generalHeader, in MethodFrameHeader methodHeader, out int consumed)
    {
        switch (methodHeader.ClassId)
        {
            case EClassId.Connection:
                return TryReadConnectionMethod(data, methodHeader, out consumed);
            case EClassId.Channel:
                return TryReadChannelMethod(data, generalHeader, methodHeader, out consumed);
            case EClassId.Queue:
                return TryReadQueueMethod(data, generalHeader, methodHeader, out consumed);
            default:
                throw new NotSupportedException(methodHeader.ClassId.ToString());
        }
        return false;
    }

    private bool TryReadConnectionMethod(in ReadOnlySequence<byte> data, in MethodFrameHeader header, out int consumed)
    {
        var methodId = (EConnectionMethodId)header.MethodId;
        switch (methodId)
        {
            case EConnectionMethodId.StartOk:
                {
                    if (ConnectionStarted.TryDeserialize(data, out var msg, out consumed))
                    {
                        _receivedMessages.OnNext(msg);
                        Buffer(new TuneConnection());
                        return true;
                    }
                    break;
                }
            case EConnectionMethodId.TuneOk:
                {
                    if (ConnectionTuned.TryDeserialize(data, out var msg, out consumed))
                    {
                        _receivedMessages.OnNext(msg);
                        return true;
                    }
                    break;
                }
            case EConnectionMethodId.Open:
                {
                    if (OpenConnection.TryDeserialize(data, out var msg, out consumed))
                    {
                        VirtualHost = msg.VirtualHost;
                        _receivedMessages.OnNext(msg);
                        Buffer(new ConnectionOpened() { VirtualHost = msg.VirtualHost });
                        return true;
                    }
                    break;
                }
            default:
                throw new NotSupportedException(methodId.ToString());
        }
        return false;
    }

    private bool TryReadQueueMethod(in ReadOnlySequence<byte> data, in GeneralFrameHeader generalHeader, in MethodFrameHeader methodHeader, out int consumed)
    {
        var methodId = (EQueueMethodId)methodHeader.MethodId;
        switch (methodId)
        {
            case EQueueMethodId.Declare:
                {
                    if (DeclareQueue.TryDeserialize(data, out var msg, out consumed))
                    {
                        ImmutableInterlockedEx.Update(ref _channels, generalHeader.Channel, current => current with 
                        { 
                            Queues = current.Queues.Add(msg.Queue, new AmqpQueue()
                            {
                                Name = msg.Queue,
                                Passive = msg.Passive,
                                Durable = msg.Durable,
                                Exclusive = msg.Exclusive,
                                AutoDelete = msg.AutoDelete,
                                Nowait = msg.Nowait,
                                Arguments = msg.Arguments.ToImmutableDictionary(),
                            })
                        });
                        _receivedMessages.OnNext(msg);
                        Buffer(new QueueDeclared() { Channel = generalHeader.Channel, Queue = msg.Queue });
                        return true;
                    }
                    break;
                }
            default:
                throw new NotSupportedException(methodId.ToString());
        }
        return false;
    }

    private bool TryReadChannelMethod(in ReadOnlySequence<byte> data, in GeneralFrameHeader generalHeader, in MethodFrameHeader methodHeader, out int consumed)
    {
        var methodId = (EChannelMethodId)methodHeader.MethodId;
        switch (methodId)
        {
            case EChannelMethodId.Open:
                {
                    if (OpenChannel.TryDeserialize(data, out var msg, out consumed))
                    {
                        _channels = _channels.Add(generalHeader.Channel, new AmqpChannel() { Id = generalHeader.Channel });
                        _receivedMessages.OnNext(msg);
                        Buffer(new ChannelOpened() { Channel = generalHeader.Channel });
                        return true;
                    }
                    break;
                }
            case EChannelMethodId.Flow:
                {
                    if (ConnectionTuned.TryDeserialize(data, out var msg, out consumed))
                    {
                        _receivedMessages.OnNext(msg);
                        return true;
                    }
                    break;
                }
            default:
                throw new NotSupportedException(methodId.ToString());
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
        Write(message);
        await _pipeWriter.FlushAsync(_cancellationToken);
    }

    private void Buffer<T>(T message) where T : IMessage
    {
        if (!_writeBuffer.Writer.TryWrite(message)) 
        {
            _writeBuffer.Writer.WriteAsync(message).GetAwaiter().GetResult();
        }
    }

    private void Write<T>(T message) where T : IMessage
    {
        var writer = new ArrayBufferWriter<byte>();
        message.Serialize(writer);
        new GeneralFrameHeader() { FrameHeaderType = EFrameHeaderType.METHOD, Length = writer.WrittenCount, Channel = message.Channel }.Serialize(_pipeWriter);
        _pipeWriter.Write(writer.WrittenSpan);
        _pipeWriter.WriteByte(0xce);
    }
}
