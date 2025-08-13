using System.Threading.Channels;
using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

public interface IPaymentQueue
{
    ValueTask<bool> EnqueueAsync(PaymentQueueItem item, CancellationToken cancellationToken = default);
    IAsyncEnumerable<PaymentQueueItem> DequeueAsync(CancellationToken cancellationToken = default);
}

public sealed class PaymentQueue : IPaymentQueue
{
    private readonly Channel<PaymentQueueItem> _channel;
    private readonly ChannelWriter<PaymentQueueItem> _writer;
    private readonly ChannelReader<PaymentQueueItem> _reader;

    public PaymentQueue(int capacity = 10000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        };

        _channel = Channel.CreateBounded<PaymentQueueItem>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
        
        Console.WriteLine($"PaymentQueue inicializada com capacidade {capacity}");
    }

    public async ValueTask<bool> EnqueueAsync(PaymentQueueItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await _writer.WriteAsync(item, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    public async IAsyncEnumerable<PaymentQueueItem> DequeueAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in _reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }
}
