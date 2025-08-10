using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface principal para processamento de pagamentos
/// Integra database, circuit breakers e HTTP clients
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processa pagamento com estratégia default->fallback
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém resumo dos pagamentos para auditoria
    /// </summary>
    Task<PaymentsSummary> GetPaymentsSummaryAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se pagamento já foi processado (idempotência)
    /// </summary>
    Task<bool> IsPaymentProcessedAsync(Guid correlationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Resultado do processamento de pagamento
/// </summary>
public sealed record PaymentResult
{
    public required bool Success { get; init; }
    public required string ProcessorUsed { get; init; } // "default", "fallback", ou "none"
    public required PaymentRecord? PaymentRecord { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Resultado de sucesso
    /// </summary>
    public static PaymentResult Successful(string processorUsed, PaymentRecord paymentRecord)
    {
        return new PaymentResult
        {
            Success = true,
            ProcessorUsed = processorUsed,
            PaymentRecord = paymentRecord
        };
    }

    /// <summary>
    /// Resultado de falha
    /// </summary>
    public static PaymentResult Failed(string errorMessage)
    {
        return new PaymentResult
        {
            Success = false,
            ProcessorUsed = "none",
            PaymentRecord = null,
            ErrorMessage = errorMessage
        };
    }
}
