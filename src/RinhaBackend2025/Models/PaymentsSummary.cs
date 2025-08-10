using System.Text.Json.Serialization;

namespace RinhaBackend2025.Models;

/// <summary>
/// Response DTO para endpoint de auditoria
/// Formato exato exigido pela competicao
/// </summary>
public sealed record PaymentsSummary
{
    [JsonPropertyName("default")]
    public required ProcessorSummary Default { get; init; }

    [JsonPropertyName("fallback")]
    public required ProcessorSummary Fallback { get; init; }

    /// <summary>
    /// Cria summary vazio para inicializacao
    /// </summary>
    public static PaymentsSummary Empty => new()
    {
        Default = ProcessorSummary.Empty,
        Fallback = ProcessorSummary.Empty
    };
}

/// <summary>
/// Resumo por processador individual
/// </summary>
public sealed record ProcessorSummary
{
    [JsonPropertyName("totalRequests")]
    public required int TotalRequests { get; init; }

    [JsonPropertyName("totalAmount")]
    public required decimal TotalAmount { get; init; }

    /// <summary>
    /// Summary vazio
    /// </summary>
    public static ProcessorSummary Empty => new()
    {
        TotalRequests = 0,
        TotalAmount = 0m
    };

    /// <summary>
    /// Cria summary a partir de dados agregados
    /// </summary>
    public static ProcessorSummary Create(int totalRequests, decimal totalAmount)
    {
        return new ProcessorSummary
        {
            TotalRequests = totalRequests,
            TotalAmount = totalAmount
        };
    }
}
