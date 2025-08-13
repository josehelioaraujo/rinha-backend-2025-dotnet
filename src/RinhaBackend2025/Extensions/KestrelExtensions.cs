using Microsoft.AspNetCore.Server.Kestrel.Core;
using RinhaBackend2025.Services;

namespace RinhaBackend2025.Extensions;

/// <summary>
/// Extensões para tuning ultra-agressivo do Kestrel
/// Otimizado para >10k RPS com p99 < 1.3ms
/// </summary>
public static class KestrelExtensions
{
    /// <summary>
    /// Configura Kestrel para performance máxima
    /// </summary>
    public static IServiceCollection AddKestrelOptimizations(this IServiceCollection services)
    {
        services.Configure<KestrelServerOptions>(options =>
        {
            // HTTP/1.1 otimizado para máxima performance
            options.ConfigureEndpointDefaults(listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http1;
            });

            // Limits ultra-agressivos
            options.Limits.MaxConcurrentConnections = 50000;
            options.Limits.MaxConcurrentUpgradedConnections = 50000;
            options.Limits.MaxRequestBodySize = 1024; // 1KB máximo por request
            options.Limits.MaxRequestBufferSize = 1024;
            options.Limits.MaxRequestLineSize = 1024;
            options.Limits.MaxRequestHeadersTotalSize = 8192;
            options.Limits.MaxRequestHeaderCount = 30;

            // Timeouts agressivos
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(5);
            options.Limits.MaxResponseBufferSize = 1024;

            // Otimizações de conexão
            options.AllowSynchronousIO = false; // Força async
            options.DisableStringReuse = false; // Reusa strings
        });

        return services;
    }

    /// <summary>
    /// Adiciona cache in-memory otimizado
    /// </summary>
    public static IServiceCollection AddMemoryCacheOptimizations(this IServiceCollection services)
    {
        services.AddMemoryCache(options =>
        {
            // Cache compacto para máxima performance
            options.SizeLimit = 1000; // Máximo 1000 entradas
            options.CompactionPercentage = 0.25; // Remove 25% quando cheio
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }

    /// <summary>
    /// Adiciona middleware de performance
    /// </summary>
    public static WebApplication UsePerformanceOptimizations(this WebApplication app)
    {
        // Ordem crítica dos middlewares para performance máxima
        
        // Compression primeiro (se habilitado)
        // app.UseResponseCompression(); // Desabilitado para performance

        // Routing otimizado
        app.UseRouting();

        // Custom headers para cache
        app.Use(async (context, next) =>
        {
            // Headers de performance
            context.Response.Headers["Server"] = "Rinha2025";
            context.Response.Headers["X-Powered-By"] = "RinhaBackend2025";
            
            // Cache headers para endpoints estáticos
            if (context.Request.Path.StartsWithSegments("/metrics") || 
                context.Request.Path.StartsWithSegments("/stats"))
            {
                context.Response.Headers["Cache-Control"] = "no-cache";
            }

            await next();
        });

        return app;
    }

    /// <summary>
    /// Adiciona endpoints de cache
    /// </summary>
    public static WebApplication MapCacheEndpoints(this WebApplication app)
    {
        app.MapGet("/metrics/cache", (ICacheService cache) =>
        {
            var stats = cache.GetStats();
            return Results.Ok(stats);
        })
        .WithName("GetCacheMetrics")
        .WithTags("Metrics");

        app.MapPost("/admin/cache/invalidate", (ICacheService cache) =>
        {
            cache.InvalidateSummaryCache();
            return Results.Ok(new { message = "Cache invalidated" });
        })
        .WithName("InvalidateCache")
        .WithTags("Admin");

        app.MapPost("/admin/cache/warmup", async (ICacheService cache) =>
        {
            await cache.WarmupAsync();
            return Results.Ok(new { message = "Cache warmed up" });
        })
        .WithName("WarmupCache")
        .WithTags("Admin");

        return app;
    }
}
