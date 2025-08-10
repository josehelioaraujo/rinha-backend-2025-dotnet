using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface para cliente dos payment processors
/// </summary>
public interface IPaymentProcessorClient
{
    /// <summary>
    /// Processa pagamento no processador default
    /// </summary>
    Task<PaymentProcessorResponse?> ProcessPaymentDefaultAsync(PaymentProcessorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processa pagamento no processador fallback
    /// </summary>
    Task<PaymentProcessorResponse?> ProcessPaymentFallbackAsync(PaymentProcessorRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém health check do processador
    /// </summary>
    Task<HealthResponse?> GetHealthAsync(string processorName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se processador está saudável
    /// </summary>
    Task<bool> IsHealthyAsync(string processorName, CancellationToken cancellationToken = default);
}
