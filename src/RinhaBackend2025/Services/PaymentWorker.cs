using RinhaBackend2025.Models;
using System.Diagnostics;

namespace RinhaBackend2025.Services;

/// <summary>
/// Background worker que processa pagamentos da queue
/// </summary>
public sealed class PaymentWorker : BackgroundService
{
    private readonly PaymentQueue _queue;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentWorker> _logger;
    private readonly int _workerId;

    public PaymentWorker(
        PaymentQueue queue,
        IPaymentService paymentService,
        ILogger<PaymentWorker> logger,
        int workerId = 0)
    {
        _queue = queue;
        _paymentService = paymentService;
        _logger = logger;
        _workerId = workerId;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentWorker {WorkerId} iniciado", _workerId);

        var reader = _queue.GetReader();

        try
        {
            await foreach (var item in reader.ReadAllAsync(stoppingToken))
            {
                await ProcessPaymentItemAsync(item);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("PaymentWorker {WorkerId} cancelado", _workerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no PaymentWorker {WorkerId}", _workerId);
        }

        _logger.LogInformation("PaymentWorker {WorkerId} finalizado", _workerId);
    }

    private async Task ProcessPaymentItemAsync(PaymentQueueItem item)
    {
        var sw = Stopwatch.StartNew();
        PaymentResult? result = null;
        bool success = false;

        try
        {
            result = await _paymentService.ProcessPaymentAsync(item.PaymentRequest, item.CancellationToken);
            success = result.Success;

            // Se tem CompletionSource, definir resultado
            if (item.CompletionSource != null)
            {
                item.CompletionSource.SetResult(result);
            }
        }
        catch (OperationCanceledException) when (item.CancellationToken.IsCancellationRequested)
        {
            result = PaymentResult.Failed("Cancelled");
            item.CompletionSource?.SetCanceled(item.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro processando pagamento {CorrelationId} no worker {WorkerId}", 
                item.PaymentRequest.CorrelationId, _workerId);

            result = PaymentResult.Failed($"Worker error: {ex.Message}");
            item.CompletionSource?.SetResult(result);
        }
        finally
        {
            sw.Stop();
            _queue.RecordProcessed(success, sw.ElapsedMilliseconds);

            if (sw.ElapsedMilliseconds > 1000) // Log slow processing
            {
                _logger.LogWarning("Processamento lento: {CorrelationId} levou {ElapsedMs}ms no worker {WorkerId}",
                    item.PaymentRequest.CorrelationId, sw.ElapsedMilliseconds, _workerId);
            }
        }
    }
}
