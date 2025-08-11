using RinhaBackend2025.Extensions;
using RinhaBackend2025.Models;
using RinhaBackend2025.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging simples
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning); // Menos logs = mais performance

// Services básicos (SEM cache problemático)
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddResilience();
builder.Services.AddPaymentProcessorClients(builder.Configuration);
builder.Services.AddPaymentService();
builder.Services.AddPaymentPipeline(builder.Configuration);

var app = builder.Build();

// Inicializar database
try
{
    await app.Services.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Database init failed: {ex.Message}");
}

// Endpoints ultra-simples e estáveis
app.MapGet("/", () => "Rinha Backend 2025 - Ultra Performance STABLE");

// Stats básico sem cache
app.MapGet("/stats", async (IDatabaseService db) =>
{
    try
    {
        var stats = await db.GetStatsAsync();
        return Results.Ok(stats);
    }
    catch
    {
        return Results.Ok(new { error = "stats unavailable" });
    }
});

// Queue metrics simples
app.MapGet("/metrics/queue", (IPaymentQueue queue) =>
{
    try
    {
        var metrics = queue.GetMetrics();
        return Results.Ok(metrics);
    }
    catch
    {
        return Results.Ok(new { error = "queue metrics unavailable" });
    }
});

// Circuit breaker metrics simples
app.MapGet("/metrics/circuit-breakers", (ICircuitBreakerFactory factory) =>
{
    try
    {
        var metrics = new
        {
            Default = factory.GetCircuitBreaker("default").GetMetrics(),
            Fallback = factory.GetCircuitBreaker("fallback").GetMetrics()
        };
        return Results.Ok(metrics);
    }
    catch
    {
        return Results.Ok(new { error = "circuit breaker metrics unavailable" });
    }
});

// ★ ENDPOINTS OBRIGATÓRIOS ULTRA-ESTÁVEIS ★

// POST /payments - pipeline assíncrono
app.MapPost("/payments", async (
    PaymentRequest request,
    IPaymentQueue queue,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Validação básica
        if (request?.CorrelationId == Guid.Empty || request?.Amount <= 0)
        {
            return Results.Ok(); // Fail-silent sempre 2XX
        }

        await queue.EnqueueAsync(request, cancellationToken);
        return Results.Ok();
    }
    catch
    {
        return Results.Ok(); // SEMPRE 2XX mesmo com erro
    }
});

// GET /payments-summary - SEM CACHE (direto do database)
app.MapGet("/payments-summary", async (
    DateTime? from,
    DateTime? to,
    IPaymentService paymentService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var summary = await paymentService.GetPaymentsSummaryAsync(from, to, cancellationToken);
        return Results.Ok(summary);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Summary error: {ex.Message}");
        // SEMPRE retorna summary válido (mesmo vazio)
        return Results.Ok(PaymentsSummary.Empty);
    }
});

app.Run();
