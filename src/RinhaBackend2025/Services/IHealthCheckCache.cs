using RinhaBackend2025.Models;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface para cache de health checks
/// Respeita rate limit de 5 segundos dos payment processors
/// </summary>
public interface IHealthCheckCache
{
    /// <summary>
    /// Obtém health response (cache ou nova requisição)
    /// </summary>
    Task<HealthResponse?> GetHealthAsync(string processorName);

    /// <summary>
    /// Força refresh do cache (usar com moderação)
    /// </summary>
    Task RefreshCacheAsync(string processorName);

    /// <summary>
    /// Verifica se cache está válido
    /// </summary>
    bool IsCacheValid(string processorName);
}
