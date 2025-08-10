using RinhaBackend2025.Services;

namespace RinhaBackend2025.Models;

/// <summary>
/// Item da queue de processamento assíncrono
/// Otimizado para bounded channels
/// </summary>
public sealed record PaymentQueueItem
{
    public required PaymentRequest PaymentRequest { get; init; }
    public required DateTime QueuedAt { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public TaskCompletionSource<PaymentResult>? CompletionSource { get; init; }

    /// <summary>
    /// Cria item para processamento fire-and-forget
    /// </summary>
    public static PaymentQueueItem CreateFireAndForget(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        return new PaymentQueueItem
        {
            PaymentRequest = request,
            QueuedAt = DateTime.UtcNow,
            CancellationToken = cancellationToken,
            CompletionSource = null // Fire-and-forget
        };
    }

    /// <summary>
    /// Cria item para processamento com await
    /// </summary>
    public static PaymentQueueItem CreateWithCompletion(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        return new PaymentQueueItem
        {
            PaymentRequest = request,
            QueuedAt = DateTime.UtcNow,
            CancellationToken = cancellationToken,
            CompletionSource = new TaskCompletionSource<PaymentResult>()
        };
    }

    /// <summary>
    /// Tempo na queue
    /// </summary>
    public TimeSpan QueueTime => DateTime.UtcNow - QueuedAt;
}
