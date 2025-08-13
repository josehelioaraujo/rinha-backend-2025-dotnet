using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface para database service otimizado
/// </summary>
public interface IDatabaseService : IDisposable
{
    /// <summary>
    /// Inicializa database e schema
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Salva pagamento de forma atomica
    /// </summary>
    Task SavePaymentAsync(PaymentRecord payment);

    /// <summary>
    /// Obtem resumo dos pagamentos para auditoria
    /// </summary>
    Task<PaymentsSummary> GetPaymentsSummaryAsync(DateTime? from, DateTime? to);

    /// <summary>
    /// Verifica se pagamento ja existe (idempotencia)
    /// </summary>
    Task<bool> PaymentExistsAsync(Guid correlationId);

    /// <summary>
    /// Obtem estatisticas do database
    /// </summary>
    Task<DatabaseStats> GetStatsAsync();
}

/// <summary>
/// Estatisticas do database para monitoramento
/// </summary>
public sealed record DatabaseStats
{
    public required int TotalPayments { get; init; }
    public required int DefaultPayments { get; init; }
    public required int FallbackPayments { get; init; }
    public required decimal TotalAmount { get; init; }
    public required long DatabaseSizeBytes { get; init; }
}
