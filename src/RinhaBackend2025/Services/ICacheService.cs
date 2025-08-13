using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface para cache in-memory híbrido
/// Otimizado para queries sub-millisecond
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Obtém summary do cache (se disponível)
    /// </summary>
    Task<PaymentsSummary?> GetSummaryAsync(DateTime? from, DateTime? to);

    /// <summary>
    /// Invalida cache do summary
    /// </summary>
    void InvalidateSummaryCache();

    /// <summary>
    /// Obtém estatísticas do cache
    /// </summary>
    CacheStats GetStats();

    /// <summary>
    /// Pré-aquece cache com dados importantes
    /// </summary>
    Task WarmupAsync();
}

/// <summary>
/// Estatísticas do cache
/// </summary>
public sealed record CacheStats
{
    public required int TotalHits { get; init; }
    public required int TotalMisses { get; init; }
    public required int CachedItems { get; init; }
    public required double HitRatio { get; init; }
    public required DateTime LastUpdated { get; init; }
    
    public static CacheStats Empty => new()
    {
        TotalHits = 0,
        TotalMisses = 0,
        CachedItems = 0,
        HitRatio = 0.0,
        LastUpdated = DateTime.MinValue
    };
}
