using RinhaBackend2025.Models;
using System.Threading.Channels;

namespace RinhaBackend2025.Services;

/// <summary>
/// Queue de pagamentos usando bounded channels
/// Otimizada para alta throughput com backpressure
/// </summary>
public sealed class PaymentQueue : IPaymentQueue, IDisposable
{
    private const int DEFAULT_QUEUE_CAPACITY = 10000;
    
    private readonly Channel<PaymentQueueItem> _channel;
    private readonly ChannelWriter<PaymentQueueItem> _writer;
    private readonly ChannelReader<PaymentQueueItem> _reader;
    private readonly ILogger<PaymentQueue> _logger;

    // Métricas thread-safe com Interlocked
    private volatile int _totalEnqueued;
    private volatile int _totalProcessed;
    private volatile int _totalFailed;
    private long _totalProcessingTimeMs; // Usar Interlocked.Read/Add
    private long _lastProcessedAt; // Usar Interlocked.Read/Exchange

    public PaymentQueue(ILogger<PaymentQueue> logger, int capacity = DEFAULT_QUEUE_CAPACITY)
    {
        _logger = logger;

        // Criar bounded channel com backpressure
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false, // Múltiplos workers
            SingleWriter = false, // Múltiplos producers
            AllowSynchronousContinuations = false // Performance
        };

        _channel = Channel.CreateBounded<PaymentQueueItem>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;

        _logger.LogInformation("PaymentQueue inicializada com capacidade {Capacity}", capacity);
    }

    public async ValueTask<bool> EnqueueAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = PaymentQueueItem.CreateFireAndForget(request, cancellationToken);
            
            await _writer.WriteAsync(item, cancellationToken);
            
            Interlocked.Increment(ref _totalEnqueued);
            
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enfileirar pagamento {CorrelationId}", request.CorrelationId);
            return false;
        }
    }

    public async ValueTask<PaymentResult> EnqueueAndWaitAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var item = PaymentQueueItem.CreateWithCompletion(request, cancellationToken);
            
            await _writer.WriteAsync(item, cancellationToken);
            
            Interlocked.Increment(ref _totalEnqueued);

            // Aguardar processamento
            if (item.CompletionSource != null)
            {
                return await item.CompletionSource.Task;
            }

            return PaymentResult.Failed("CompletionSource é null");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return PaymentResult.Failed("Operation cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enfileirar e aguardar pagamento {CorrelationId}", request.CorrelationId);
            return PaymentResult.Failed($"Queue error: {ex.Message}");
        }
    }

    public QueueMetrics GetMetrics()
    {
        var processed = _totalProcessed;
        var totalTime = Interlocked.Read(ref _totalProcessingTimeMs);
        var avgTime = processed > 0 ? (double)totalTime / processed : 0;

        return new QueueMetrics
        {
            QueueLength = _reader.CanCount ? _reader.Count : -1,
            TotalEnqueued = _totalEnqueued,
            TotalProcessed = processed,
            TotalFailed = _totalFailed,
            AverageProcessingTimeMs = avgTime,
            LastProcessedAt = new DateTime(Interlocked.Read(ref _lastProcessedAt))
        };
    }

    /// <summary>
    /// Obtém reader para workers consumirem
    /// </summary>
    public ChannelReader<PaymentQueueItem> GetReader() => _reader;

    /// <summary>
    /// Registra processamento concluído (chamado pelos workers)
    /// </summary>
    public void RecordProcessed(bool success, long processingTimeMs)
    {
        if (success)
        {
            Interlocked.Increment(ref _totalProcessed);
        }
        else
        {
            Interlocked.Increment(ref _totalFailed);
        }

        Interlocked.Add(ref _totalProcessingTimeMs, processingTimeMs);
        Interlocked.Exchange(ref _lastProcessedAt, DateTime.UtcNow.Ticks);
    }

    public void Dispose()
    {
        _writer.Complete();
    }
}
