using RinhaBackend2025.Extensions;
using RinhaBackend2025.Models;
using RinhaBackend2025.Services;

var builder = WebApplication.CreateBuilder(args);

// Logging simples
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Services básicos
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddResilience();
builder.Services.AddPaymentProcessorClients(builder.Configuration);
builder.Services.AddPaymentService();
builder.Services.AddPaymentPipeline(builder.Configuration);

var app = builder.Build();

// Inicializar database
await app.Services.InitializeDatabaseAsync();

// Endpoints básicos
app.MapGet("/", () => "Rinha Backend 2025 - Ultra Performance");

app.MapGet("/stats", async (IDatabaseService db) =>
{
    var stats = await db.GetStatsAsync();
    return Results.Ok(stats);
});

app.MapGet("/metrics/queue", (IPaymentQueue queue) =>
{
    var metrics = queue.GetMetrics();
    return Results.Ok(metrics);
});

app.MapGet("/metrics/circuit-breakers", (ICircuitBreakerFactory factory) =>
{
    var metrics = new
    {
        Default = factory.GetCircuitBreaker("default").GetMetrics(),
        Fallback = factory.GetCircuitBreaker("fallback").GetMetrics()
    };
    return Results.Ok(metrics);
});

// ENDPOINTS OBRIGATÓRIOS
app.MapPost("/payments", async (
    PaymentRequest request,
    IPaymentQueue queue,
    CancellationToken cancellationToken) =>
{
    await queue.EnqueueAsync(request, cancellationToken);
    return Results.Ok();
});

app.MapGet("/payments-summary", async (
    DateTime? from,
    DateTime? to,
    IPaymentService paymentService,
    CancellationToken cancellationToken) =>
{
    var summary = await paymentService.GetPaymentsSummaryAsync(from, to, cancellationToken);
    return Results.Ok(summary);
});

app.Run();
