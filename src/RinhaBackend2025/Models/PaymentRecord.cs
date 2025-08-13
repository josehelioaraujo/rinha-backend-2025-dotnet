namespace RinhaBackend2025.Models;

/// <summary>
/// Entidade para persistencia no SQLite
/// Otimizada para armazenamento e consulta rapida
/// </summary>
public sealed record PaymentRecord
{
    public required Guid CorrelationId { get; init; }
    public required decimal Amount { get; init; }
    public required string Processor { get; init; } // "default" ou "fallback"
    public required DateTime RequestedAt { get; init; }
    public required DateTime ProcessedAt { get; init; }

    /// <summary>
    /// Converte DateTime para Ticks para melhor performance no SQLite
    /// </summary>
    public long RequestedAtTicks => RequestedAt.Ticks;
    public long ProcessedAtTicks => ProcessedAt.Ticks;

    /// <summary>
    /// Factory method para criar registro de pagamento
    /// </summary>
    public static PaymentRecord Create(
        Guid correlationId,
        decimal amount,
        string processor,
        DateTime requestedAt)
    {
        return new PaymentRecord
        {
            CorrelationId = correlationId,
            Amount = amount,
            Processor = processor,
            RequestedAt = requestedAt,
            ProcessedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria registro a partir de ticks do SQLite
    /// </summary>
    public static PaymentRecord FromTicks(
        Guid correlationId,
        decimal amount,
        string processor,
        long requestedAtTicks,
        long processedAtTicks)
    {
        return new PaymentRecord
        {
            CorrelationId = correlationId,
            Amount = amount,
            Processor = processor,
            RequestedAt = new DateTime(requestedAtTicks),
            ProcessedAt = new DateTime(processedAtTicks)
        };
    }
}
