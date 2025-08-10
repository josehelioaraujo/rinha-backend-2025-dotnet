using RinhaBackend2025.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace RinhaBackend2025.Services;

/// <summary>
/// Cache inteligente para health checks com rate limiting
/// </summary>
public sealed class HealthCheckCache : IHealthCheckCache
{
    private const int CACHE_DURATION_SECONDS = 5;
    private const string HEALTH_ENDPOINT = "/payments/service-health";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthCheckCache> _logger;
    private readonly ConcurrentDictionary<string, CachedHealthResponse> _cache;

    public HealthCheckCache(IHttpClientFactory httpClientFactory, ILogger<HealthCheckCache> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _cache = new ConcurrentDictionary<string, CachedHealthResponse>();
    }

    public async Task<HealthResponse?> GetHealthAsync(string processorName)
    {
        var now = DateTime.UtcNow;
        
        // Verificar cache primeiro
        if (_cache.TryGetValue(processorName, out var cached))
        {
            if (now.Subtract(cached.Timestamp).TotalSeconds < CACHE_DURATION_SECONDS)
            {
                return cached.Health;
            }
        }

        // Cache expirado ou não existe, fazer nova requisição
        try
        {
            var client = _httpClientFactory.CreateClient(processorName);
            
            using var response = await client.GetAsync(HEALTH_ENDPOINT);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var health = JsonSerializer.Deserialize<HealthResponse>(json);
                
                if (health != null)
                {
                    // Atualizar cache
                    _cache.AddOrUpdate(processorName, 
                        new CachedHealthResponse(health, now),
                        (key, old) => new CachedHealthResponse(health, now));
                    
                    return health;
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit atingido para {ProcessorName}, usando cache", processorName);
                
                // Retornar cache mesmo expirado se tiver rate limit
                if (cached != null)
                    return cached.Health;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter health check de {ProcessorName}", processorName);
            
            // Em caso de erro, retornar cache se disponível
            if (cached != null)
                return cached.Health;
        }

        return null;
    }

    public async Task RefreshCacheAsync(string processorName)
    {
        // Remove cache forçando nova requisição
        _cache.TryRemove(processorName, out _);
        await GetHealthAsync(processorName);
    }

    public bool IsCacheValid(string processorName)
    {
        if (!_cache.TryGetValue(processorName, out var cached))
            return false;

        return DateTime.UtcNow.Subtract(cached.Timestamp).TotalSeconds < CACHE_DURATION_SECONDS;
    }

    /// <summary>
    /// Item do cache com timestamp
    /// </summary>
    private sealed record CachedHealthResponse(HealthResponse Health, DateTime Timestamp);
}
