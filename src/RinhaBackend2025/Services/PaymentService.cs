using RinhaBackend2025.Models;
using System.Runtime.CompilerServices;

namespace RinhaBackend2025.Services;

/// <summary>
/// Serviço principal de processamento de pagamentos
/// Implementa estratégia default->fallback com circuit breakers
/// </summary>
public sealed class PaymentService : IPaymentService
{
    private readonly IDatabaseService _database;
    private readonly IPaymentProcessorClient _processorClient;
    private readonly ICircuitBreakerFactory _circuitBreakerFactory;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IDatabaseService database,
        IPaymentProcessorClient processorClient,
        ICircuitBreakerFactory circuitBreakerFactory,
        ILogger<PaymentService> logger)
    {
        _database = database;
        _processorClient = processorClient;
        _circuitBreakerFactory = circuitBreakerFactory;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        PaymentRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validação rápida
        if (!request.IsValid())
        {
            return PaymentResult.Failed("Invalid payment request");
        }

        try
        {
            // Verificar idempotência (evitar reprocessamento)
            if (await _database.PaymentExistsAsync(request.CorrelationId))
            {
                _logger.LogInformation("Payment {CorrelationId} already processed (idempotent)", 
                    request.CorrelationId);
                return PaymentResult.Failed("Payment already processed");
            }

            // Tentar processador DEFAULT primeiro (menor taxa)
            var result = await TryProcessorAsync("default", request, cancellationToken);
            if (result.Success)
            {
                return result;
            }

            // Se default falhou, tentar FALLBACK (taxa maior)
            _logger.LogWarning("Default processor failed for {CorrelationId}, trying fallback", 
                request.CorrelationId);

            result = await TryProcessorAsync("fallback", request, cancellationToken);
            if (result.Success)
            {
                return result;
            }

            // Ambos processadores falharam
            _logger.LogError("Both processors failed for {CorrelationId}", request.CorrelationId);
            return PaymentResult.Failed("All payment processors unavailable");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Payment processing cancelled for {CorrelationId}", request.CorrelationId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment {CorrelationId}", request.CorrelationId);
            return PaymentResult.Failed($"Internal error: {ex.Message}");
        }
    }

    public async Task<PaymentsSummary> GetPaymentsSummaryAsync(
        DateTime? from, 
        DateTime? to, 
        CancellationToken cancellationToken = default)
    {
        return await _database.GetPaymentsSummaryAsync(from, to);
    }

    public async Task<bool> IsPaymentProcessedAsync(
        Guid correlationId, 
        CancellationToken cancellationToken = default)
    {
        return await _database.PaymentExistsAsync(correlationId);
    }

    /// <summary>
    /// Tenta processar pagamento em um processador específico
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task<PaymentResult> TryProcessorAsync(
        string processorName,
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        var circuitBreaker = _circuitBreakerFactory.GetCircuitBreaker(processorName);

        // Verificar circuit breaker antes de tentar
        if (!circuitBreaker.CanExecute())
        {
            _logger.LogWarning("Circuit breaker open for {ProcessorName}", processorName);
            return PaymentResult.Failed($"Processor {processorName} circuit breaker open");
        }

        try
        {
            // Criar request para payment processor
            var processorRequest = PaymentProcessorRequest.FromPaymentRequest(request);

            // Chamar processador via HTTP client
            PaymentProcessorResponse? response = processorName switch
            {
                "default" => await _processorClient.ProcessPaymentDefaultAsync(processorRequest, cancellationToken),
                "fallback" => await _processorClient.ProcessPaymentFallbackAsync(processorRequest, cancellationToken),
                _ => null
            };

            if (response != null)
            {
                // Sucesso - salvar no database
                var paymentRecord = PaymentRecord.Create(
                    request.CorrelationId,
                    request.Amount,
                    processorName,
                    DateTime.UtcNow
                );

                await _database.SavePaymentAsync(paymentRecord);

                _logger.LogInformation("Payment {CorrelationId} processed successfully with {ProcessorName}", 
                    request.CorrelationId, processorName);

                return PaymentResult.Successful(processorName, paymentRecord);
            }
            else
            {
                // Falha no processamento
                _logger.LogWarning("Processor {ProcessorName} failed to process {CorrelationId}", 
                    processorName, request.CorrelationId);

                return PaymentResult.Failed($"Processor {processorName} failed");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment {CorrelationId} with {ProcessorName}", 
                request.CorrelationId, processorName);

            return PaymentResult.Failed($"Processor {processorName} error: {ex.Message}");
        }
    }
}
