using System.Text.Json.Serialization;

namespace RinhaBackend2025.Models;

/// <summary>
/// Response do health check dos payment processors
/// Cache esta resposta por 5 segundos (rate limit)
/// </summary>
public sealed record HealthResponse
{
    [JsonPropertyName("failing")]
    public required bool Failing { get; init; }

    [JsonPropertyName("minResponseTime")]
    public required int MinResponseTime { get; init; }

    /// <summary>
    /// Indica se o processador esta saudavel
    /// </summary>
    public bool IsHealthy => !Failing;



     /// <summary>
    /// Estimativa de timeout baseada no minResponseTime
    /// </summary>
    public int SuggestedTimeout => MinResponseTime + 500; // +500ms de buffer
}
