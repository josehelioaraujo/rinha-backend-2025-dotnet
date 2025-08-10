using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RinhaBackend2025.Models;

/// <summary>
/// Request DTO para processamento de pagamentos
/// Otimizado para alta performance com validacao minima
/// </summary>
public sealed record PaymentRequest
{
    [JsonPropertyName("correlationId")]
    [Required]
    public required Guid CorrelationId { get; init; }

    [JsonPropertyName("amount")]
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount deve ser maior que zero")]
    public required decimal Amount { get; init; }

    /// <summary>
    /// Validacao rapida inline para hot path
    /// </summary>
    public bool IsValid() => CorrelationId != Guid.Empty && Amount > 0;
}
