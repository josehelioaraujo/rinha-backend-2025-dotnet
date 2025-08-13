using RinhaBackend2025.Models;
using RinhaBackend2025.Services;

namespace RinhaBackend2025.Extensions;

/// <summary>
/// Extensões para configuração do pipeline assíncrono
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Adiciona pipeline de processamento assíncrono
    /// </summary>
    public static IServiceCollection AddPaymentPipeline(this IServiceCollection services, IConfiguration configuration)
    {
        // Configurações do pipeline
        var queueCapacity = configuration.GetValue<int>("Pipeline:QueueCapacity", 10000);
        var workerCount = configuration.GetValue<int>("Pipeline:WorkerCount", Environment.ProcessorCount);

        // Registrar queue como singleton
        services.AddSingleton<PaymentQueue>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<PaymentQueue>>();
            return new PaymentQueue(logger, queueCapacity);
        });

        // Registrar interface
        services.AddSingleton<IPaymentQueue>(provider => provider.GetRequiredService<PaymentQueue>());

        // Registrar workers
        for (int i = 0; i < workerCount; i++)
        {
            var workerId = i;
            services.AddSingleton<IHostedService>(provider =>
            {
                var queue = provider.GetRequiredService<PaymentQueue>();
                var paymentService = provider.GetRequiredService<IPaymentService>();
                var logger = provider.GetRequiredService<ILogger<PaymentWorker>>();
                return new PaymentWorker(queue, paymentService, logger, workerId);
            });
        }

        return services;
    }

    /// <summary>
    /// Adiciona endpoints de monitoramento do pipeline
    /// </summary>
    public static WebApplication MapPipelineEndpoints(this WebApplication app)
    {
        app.MapGet("/metrics/queue", (IPaymentQueue queue) =>
        {
            var metrics = queue.GetMetrics();
            return Results.Ok(metrics);
        })
        .WithName("GetQueueMetrics")
        .WithTags("Metrics");

        app.MapPost("/test/queue-payment", async (IPaymentQueue queue) =>
        {
            var testRequest = new PaymentRequest
            {
                CorrelationId = Guid.NewGuid(),
                Amount = 100.00m
            };

            var success = await queue.EnqueueAsync(testRequest);

            return Results.Ok(new
            {
                Enqueued = success,
                TestRequest = testRequest,
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("TestQueuePayment")
        .WithTags("Test");

        return app;
    }
}
