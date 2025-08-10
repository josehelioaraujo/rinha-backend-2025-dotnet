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
        // POST /payments - Endpoint obrigatório
        app.MapPost("/payments", async (
            PaymentRequest request,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await paymentService.ProcessPaymentAsync(request, cancellationToken);
                
                // SEMPRE retorna 2XX conforme especificação
                return Results.Ok();
            }
            catch
            {
                // SEMPRE retorna 2XX mesmo com erro (fail-silent)
                return Results.Ok();
            }
        });

        // GET /payments-summary - Endpoint obrigatório  
        app.MapGet("/payments-summary", async (
            DateTime? from,
            DateTime? to,
            IPaymentService paymentService,
            CancellationToken cancellationToken) =>
        {
            var summary = await paymentService.GetPaymentsSummaryAsync(from, to, cancellationToken);
            return Results.Ok(summary);
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

        app.MapPost("/debug/test-payment", async (IPaymentService paymentService) =>
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

        return app;
    }
}
