using Microsoft.Extensions.Caching.Memory;
using RinhaBackend2025.Models;
using System.Collections.Concurrent;

namespace RinhaBackend2025.Services;

/// <summary>
/// Cache in-memory híbrido para performance máxima
/// </summary>
public sealed class MemoryCacheService : ICacheService, IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly IDatabaseService _database;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _cacheTimestamps;

    // Métricas thread-safe
    private volatile int _totalHits;
    private volatile int _totalMisses;
    private long _lastUpdated;

    private const int CACHE_DURATION_SECONDS = 30; // Cache de 30s para summary
    private const string SUMMARY_CACHE_KEY = "payments_summary";

    public MemoryCacheService(
        IMemoryCache cache,
        IDatabaseService database,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _database = database;
        _logger = logger;
        _cacheTimestamps = new ConcurrentDictionary<string, DateTime>();
        Interlocked.Exchange(ref _lastUpdated, DateTime.UtcNow.Ticks);
    }

    public async Task<PaymentsSummary?> GetSummaryAsync(DateTime? from, DateTime? to)
    {
        // Criar chave única baseada nos parâmetros
        var key = CreateSummaryKey(from, to);

        // Tentar obter do cache primeiro
        if (_cache.TryGetValue(key, out PaymentsSummary? cachedSummary))
        {
            Interlocked.Increment(ref _totalHits);
            _logger.LogDebug("Cache hit para summary: {Key}", key);
            return cachedSummary;
        }

        // Cache miss - buscar do database
        Interlocked.Increment(ref _totalMisses);
        _logger.LogDebug("Cache miss para summary: {Key}", key);

        try
        {
            var summary = await _database.GetPaymentsSummaryAsync(from, to);

            // Cachear resultado com expiração
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CACHE_DURATION_SECONDS),
                Priority = CacheItemPriority.High,
                Size = 1
            };

            _cache.Set(key, summary, cacheOptions);
            _cacheTimestamps[key] = DateTime.UtcNow;
            Interlocked.Exchange(ref _lastUpdated, DateTime.UtcNow.Ticks);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar summary do database para cache");
            return null;
        }
    }

    public void InvalidateSummaryCache()
    {
        // Remover todas as entradas de summary do cache
        var keysToRemove = _cacheTimestamps.Keys.Where(k => k.StartsWith(SUMMARY_CACHE_KEY)).ToList();
        
        foreach (var key in keysToRemove)
        {
            _cache.Remove(key);
            _cacheTimestamps.TryRemove(key, out _);
        }

        Interlocked.Exchange(ref _lastUpdated, DateTime.UtcNow.Ticks);
        _logger.LogDebug("Cache de summary invalidado");
    }

    public CacheStats GetStats()
    {
        var hits = _totalHits;
        var misses = _totalMisses;
        var total = hits + misses;
        var hitRatio = total > 0 ? (double)hits / total : 0.0;

        return new CacheStats
        {
            TotalHits = hits,
            TotalMisses = misses,
            CachedItems = _cacheTimestamps.Count,
            HitRatio = hitRatio,
            LastUpdated = new DateTime(Interlocked.Read(ref _lastUpdated))
        };
    }

    public async Task WarmupAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando warmup do cache...");

            // Pré-carregar summary geral (sem filtros)
            await GetSummaryAsync(null, null);

            // Pré-carregar summary da última hora
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            await GetSummaryAsync(oneHourAgo, null);

            _logger.LogInformation("Warmup do cache concluído");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante warmup do cache");
        }
    }

    private static string CreateSummaryKey(DateTime? from, DateTime? to)
    {
        var fromStr = from?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "null";
        var toStr = to?.ToString("yyyy-MM-ddTHH:mm:ss") ?? "null";
        return $"{SUMMARY_CACHE_KEY}:{fromStr}:{toStr}";
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }
}
