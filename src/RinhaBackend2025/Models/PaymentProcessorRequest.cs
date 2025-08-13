using System.Text.Json.Serialization;

namespace RinhaBackend2025.Models;

/// <summary>
/// Request DTO para enviar aos payment processors
/// Inclui timestamp obrigatorio
/// </summary>
public sealed record PaymentProcessorRequest
{
    [JsonPropertyName("correlationId")]
    public required Guid CorrelationId { get; init; }

    [JsonPropertyName("amount")]
    public required decimal Amount { get; init; }

    [JsonPropertyName("requestedAt")]
    public required string RequestedAt { get; init; }

    /// <summary>
    /// Cria request para payment processor
    /// </summary>
    public static PaymentProcessorRequest FromPaymentRequest(PaymentRequest request)
    {
        return new PaymentProcessorRequest
        {
            CorrelationId = request.CorrelationId,
            Amount = request.Amount,
            RequestedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    /// <summary>
    /// Cria request com timestamp customizado
    /// </summary>
    public static PaymentProcessorRequest Create(
        Guid correlationId,
        decimal amount,
        DateTime requestedAt)
    {
        return new PaymentProcessorRequest
        {
            CorrelationId = correlationId,
            Amount = amount,
            RequestedAt = requestedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }
}
