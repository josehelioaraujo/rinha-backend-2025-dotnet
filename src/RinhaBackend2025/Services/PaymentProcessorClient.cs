using RinhaBackend2025.Models;
using System.Net;
using System.Text;
using System.Text.Json;

namespace RinhaBackend2025.Services;

/// <summary>
/// Cliente HTTP otimizado para payment processors
/// Integra com circuit breakers para resiliência
/// </summary>
public sealed class PaymentProcessorClient : IPaymentProcessorClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICircuitBreakerFactory _circuitBreakerFactory;
    private readonly IHealthCheckCache _healthCheckCache;
    private readonly ILogger<PaymentProcessorClient> _logger;
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public PaymentProcessorClient(
        IHttpClientFactory httpClientFactory,
        ICircuitBreakerFactory circuitBreakerFactory,
        IHealthCheckCache healthCheckCache,
        ILogger<PaymentProcessorClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _circuitBreakerFactory = circuitBreakerFactory;
        _healthCheckCache = healthCheckCache;
        _logger = logger;
    }

    public async Task<PaymentProcessorResponse?> ProcessPaymentDefaultAsync(
        PaymentProcessorRequest request, 
        CancellationToken cancellationToken = default)
    {
        return await ProcessPaymentAsync("default", request, cancellationToken);
    }

    public async Task<PaymentProcessorResponse?> ProcessPaymentFallbackAsync(
        PaymentProcessorRequest request, 
        CancellationToken cancellationToken = default)
    {
        return await ProcessPaymentAsync("fallback", request, cancellationToken);
    }

    public async Task<HealthResponse?> GetHealthAsync(
        string processorName, 
        CancellationToken cancellationToken = default)
    {
        return await _healthCheckCache.GetHealthAsync(processorName);
    }

    public async Task<bool> IsHealthyAsync(
        string processorName, 
        CancellationToken cancellationToken = default)
    {
        var health = await GetHealthAsync(processorName, cancellationToken);
        return health?.IsHealthy ?? false;
    }

    private async Task<PaymentProcessorResponse?> ProcessPaymentAsync(
        string processorName,
        PaymentProcessorRequest request,
        CancellationToken cancellationToken)
    {
        var circuitBreaker = _circuitBreakerFactory.GetCircuitBreaker(processorName);

        // Verificar circuit breaker antes de fazer requisição
        if (!circuitBreaker.CanExecute())
        {
            _logger.LogWarning("Circuit breaker aberto para {ProcessorName}", processorName);
            return null;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(processorName);
            
            // Serializar request
            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Fazer requisição
            using var response = await client.PostAsync("/payments", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Sucesso - registrar no circuit breaker
                circuitBreaker.RecordSuccess();

                // Tentar deserializar response (opcional)
                try
                {
                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrEmpty(responseJson))
                    {
                        return JsonSerializer.Deserialize<PaymentProcessorResponse>(responseJson, JsonOptions);
                    }
                }
                catch (JsonException)
                {
                    // Response pode não ser JSON válido, mas está OK por ser 2XX
                }

                return PaymentProcessorResponse.Success;
            }
            else if (IsServerError(response.StatusCode))
            {
                // Erro 5XX - registrar falha no circuit breaker
                circuitBreaker.RecordFailure();
                _logger.LogWarning("Erro 5XX no processador {ProcessorName}: {StatusCode}", 
                    processorName, response.StatusCode);
                return null;
            }
            else
            {
                // Outros erros (4XX) - não registrar como falha do circuit breaker
                _logger.LogWarning("Erro {StatusCode} no processador {ProcessorName}", 
                    response.StatusCode, processorName);
                return null;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Timeout cancelado para {ProcessorName}", processorName);
            throw;
        }
        catch (TaskCanceledException)
        {
            // Timeout - registrar como falha
            circuitBreaker.RecordFailure();
            _logger.LogWarning("Timeout no processador {ProcessorName}", processorName);
            return null;
        }
        catch (HttpRequestException ex)
        {
            // Erro de conexão - registrar como falha
            circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Erro de conexão no processador {ProcessorName}", processorName);
            return null;
        }
        catch (Exception ex)
        {
            // Outros erros - registrar como falha
            circuitBreaker.RecordFailure();
            _logger.LogError(ex, "Erro inesperado no processador {ProcessorName}", processorName);
            return null;
        }
    }

    private static bool IsServerError(HttpStatusCode statusCode)
    {
        return (int)statusCode >= 500 && (int)statusCode < 600;
    }
}
