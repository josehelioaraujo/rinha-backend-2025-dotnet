namespace RinhaBackend2025.Models;

// Definição limpa como record
public record PaymentResponse
{
    public string CorrelationId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
    public string ProcessorId { get; init; } = string.Empty;
}
