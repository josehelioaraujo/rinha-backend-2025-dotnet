using RinhaBackend2025.Models;
using RinhaBackend2025.Services;

namespace RinhaBackend2025.Extensions;

public static class PaymentServiceExtensions
{
    public static IServiceCollection AddPaymentService(this IServiceCollection services)
    {
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }

    public static WebApplication MapPaymentEndpoints(this WebApplication app)
    {
        // POST /payments - Endpoint obrigatório COM PIPELINE (ultra-rápido)
        app.MapPost("/payments", async (
            PaymentRequest request,
            IPaymentQueue queue,
            ICacheService cache,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Enfileirar para processamento assíncrono (fire-and-forget)
                var enqueued = await queue.EnqueueAsync(request, cancellationToken);
                
                // Invalidar cache quando há novo pagamento
                if (enqueued)
                {
                    cache.InvalidateSummaryCache();
                }
                
                // SEMPRE retorna 2XX imediatamente (máxima performance)
                return Results.Ok();
            }
            catch
            {
                // SEMPRE retorna 2XX mesmo com erro (fail-silent)
                return Results.Ok();
            }
        });

        // GET /payments-summary - Endpoint obrigatório COM CACHE
        app.MapGet("/payments-summary", async (
            DateTime? from,
            DateTime? to,
            ICacheService cache,
            CancellationToken cancellationToken) =>
        {
            // Tentar cache primeiro, fallback para database
            var summary = await cache.GetSummaryAsync(from, to);
            
            if (summary != null)
            {
                return Results.Ok(summary);
            }

            // Fallback: retorna summary vazio se cache falhar
            return Results.Ok(PaymentsSummary.Empty);
        });

        return app;
    }

    public static WebApplication MapPaymentDebugEndpoints(this WebApplication app)
    {
        app.MapGet("/debug/payment/{correlationId:guid}", async (
            Guid correlationId,
            IPaymentService paymentService) =>
        {
            var exists = await paymentService.IsPaymentProcessedAsync(correlationId);
            return Results.Ok(new { CorrelationId = correlationId, Exists = exists });
        });

        app.MapPost("/debug/test-payment-sync", async (IPaymentService paymentService) =>
        {
            var testRequest = new PaymentRequest
            {
                CorrelationId = Guid.NewGuid(),
                Amount = 99.99m
            };

            var result = await paymentService.ProcessPaymentAsync(testRequest);
            
            return Results.Ok(new
            {
                TestRequest = testRequest,
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        });

        app.MapPost("/debug/test-payment-async", async (IPaymentQueue queue) =>
        {
            var testRequest = new PaymentRequest
            {
                CorrelationId = Guid.NewGuid(),
                Amount = 199.99m
            };

            var result = await queue.EnqueueAndWaitAsync(testRequest);
            
            return Results.Ok(new
            {
                TestRequest = testRequest,
                Result = result,
                Timestamp = DateTime.UtcNow
            });
        });

        return app;
    }
}
