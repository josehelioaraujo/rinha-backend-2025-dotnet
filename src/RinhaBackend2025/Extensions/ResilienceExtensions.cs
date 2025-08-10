using RinhaBackend2025.Services;

namespace RinhaBackend2025.Extensions;

/// <summary>
/// Extensões para configuração de resiliência
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Adiciona services de resiliência (circuit breaker + health cache)
    /// </summary>
    public static IServiceCollection AddResilience(this IServiceCollection services)
    {
        // Circuit breakers
        services.AddSingleton<ICircuitBreakerFactory, CircuitBreakerFactory>();

        // Health check cache
        services.AddSingleton<IHealthCheckCache, HealthCheckCache>();

        return services;
    }

    /// <summary>
    /// Adiciona endpoint de métricas de circuit breakers
    /// </summary>
    public static WebApplication MapCircuitBreakerMetrics(this WebApplication app)
    {
        app.MapGet("/metrics/circuit-breakers", (ICircuitBreakerFactory factory) =>
        {
            var metrics = new
            {
                Default = factory.GetCircuitBreaker("default").GetMetrics(),
                Fallback = factory.GetCircuitBreaker("fallback").GetMetrics(),
                Timestamp = DateTime.UtcNow
            };

            return Results.Ok(metrics);
        });

        return app;
    }
}
