using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface para queue de pagamentos assíncrona
/// </summary>
public interface IPaymentQueue
{
    /// <summary>
    /// Envia pagamento para processamento (fire-and-forget)
    /// </summary>
    ValueTask<bool> EnqueueAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envia pagamento e aguarda resultado
    /// </summary>
    ValueTask<PaymentResult> EnqueueAndWaitAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém métricas da queue
    /// </summary>
    QueueMetrics GetMetrics();
}

/// <summary>
/// Métricas da queue para monitoramento
/// </summary>
public sealed record QueueMetrics
{
    public required int QueueLength { get; init; }
    public required int TotalEnqueued { get; init; }
    public required int TotalProcessed { get; init; }
    public required int TotalFailed { get; init; }
    public required double AverageProcessingTimeMs { get; init; }
    public required DateTime LastProcessedAt { get; init; }
    
    public static QueueMetrics Empty => new()
    {
        QueueLength = 0,
        TotalEnqueued = 0,
        TotalProcessed = 0,
        TotalFailed = 0,
        AverageProcessingTimeMs = 0,
        LastProcessedAt = DateTime.MinValue
    };
}
