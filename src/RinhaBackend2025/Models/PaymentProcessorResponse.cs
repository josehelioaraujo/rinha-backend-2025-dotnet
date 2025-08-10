using System.Text.Json.Serialization;

namespace RinhaBackend2025.Models;

/// <summary>
/// Response dos payment processors
/// Pode ser ignorado se status for 2XX
/// </summary>
public sealed record PaymentProcessorResponse
{
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>
    /// Response padrao de sucesso
    /// </summary>
    public static PaymentProcessorResponse Success => new()
    {
        Message = "payment processed successfully"
    };
}
