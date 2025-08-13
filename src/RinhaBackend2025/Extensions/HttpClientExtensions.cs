using RinhaBackend2025.Models;
using RinhaBackend2025.Services;

namespace RinhaBackend2025.Extensions;

/// <summary>
/// Extensões para configuração de HTTP clients otimizados
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Adiciona HTTP clients otimizados para payment processors
    /// </summary>
    public static IServiceCollection AddPaymentProcessorClients(this IServiceCollection services, IConfiguration configuration)
    {
        // URLs dos processadores
        var defaultUrl = configuration["PaymentProcessors:DefaultUrl"] ?? "http://payment-processor-default:8080";
        var fallbackUrl = configuration["PaymentProcessors:FallbackUrl"] ?? "http://payment-processor-fallback:8080";

        // Cliente para processador DEFAULT
        services.AddHttpClient("default", client =>
        {
            client.BaseAddress = new Uri(defaultUrl);
            client.Timeout = TimeSpan.FromMilliseconds(1500); // 1.5s - agressivo para fail-fast
            client.DefaultRequestHeaders.Add("User-Agent", "RinhaBackend2025/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateOptimizedHandler());

        // Cliente para processador FALLBACK  
        services.AddHttpClient("fallback", client =>
        {
            client.BaseAddress = new Uri(fallbackUrl);
            client.Timeout = TimeSpan.FromMilliseconds(3000); // 3s - mais tolerante
            client.DefaultRequestHeaders.Add("User-Agent", "RinhaBackend2025/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateOptimizedHandler());

        // Cliente para HEALTH CHECKS (reutiliza configurações)
        services.AddHttpClient("health-default", client =>
        {
            client.BaseAddress = new Uri(defaultUrl);
            client.Timeout = TimeSpan.FromMilliseconds(2000);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateOptimizedHandler());

        services.AddHttpClient("health-fallback", client =>
        {
            client.BaseAddress = new Uri(fallbackUrl);
            client.Timeout = TimeSpan.FromMilliseconds(2000);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateOptimizedHandler());

        // Registrar serviço principal
        services.AddSingleton<IPaymentProcessorClient, PaymentProcessorClient>();

        return services;
    }

    /// <summary>
    /// Cria handler HTTP otimizado para máxima performance
    /// </summary>
    private static SocketsHttpHandler CreateOptimizedHandler()
    {
        return new SocketsHttpHandler
        {
            // Connection pooling agressivo
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 50,
            
            // Timeouts otimizados
            ConnectTimeout = TimeSpan.FromMilliseconds(1000),
            
            // Desabilitar proxy para performance
            UseProxy = false,
            
            // HTTP/2 se disponível
            EnableMultipleHttp2Connections = true,
            
            // Evitar delay de Nagle
            UseCookies = false
        };
    }

    /// <summary>
    /// Adiciona endpoint de teste dos HTTP clients
    /// </summary>
    public static WebApplication MapHttpClientTests(this WebApplication app)
    {
        app.MapGet("/test/http-clients", async (IPaymentProcessorClient client) =>
        {
            var results = new
            {
                DefaultHealthy = await client.IsHealthyAsync("default"),
                FallbackHealthy = await client.IsHealthyAsync("fallback"),
                Timestamp = DateTime.UtcNow
            };

            return Results.Ok(results);
        });

        app.MapPost("/test/payment/{processor}", async (string processor, IPaymentProcessorClient client) =>
        {
            try
            {
                var testRequest = PaymentProcessorRequest.Create(
                    Guid.NewGuid(),
                    10.50m,
                    DateTime.UtcNow
                );

                PaymentProcessorResponse? response = processor.ToLower() switch
                {
                    "default" => await client.ProcessPaymentDefaultAsync(testRequest),
                    "fallback" => await client.ProcessPaymentFallbackAsync(testRequest),
                    _ => null
                };

                return Results.Ok(new
                {
                    Processor = processor,
                    Success = response is not null,
                    Response = response
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { Error = ex.Message });
            }
        });

        return app;
    }
}
