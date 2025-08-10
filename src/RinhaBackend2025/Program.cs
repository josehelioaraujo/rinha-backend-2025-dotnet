using RinhaBackend2025.Extensions;
using RinhaBackend2025.Models;
using RinhaBackend2025.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// Configurar database
builder.Services.AddDatabase(builder.Configuration);

// Configurar resiliência (circuit breakers + health cache)
builder.Services.AddResilience();

// TODO: Add HTTP clients configuration
// TODO: Add payment processors
// TODO: Add channel pipeline

var app = builder.Build();

// Inicializar database na startup
await app.Services.InitializeDatabaseAsync();

// TODO: Add middleware pipeline

// Health check endpoint
app.MapGet("/", () => "Rinha Backend 2025 - .NET Ultra Performance");

// Endpoint de teste do database
app.MapGet("/stats", async (IDatabaseService db) =>
{
    var stats = await db.GetStatsAsync();
    return Results.Ok(stats);
});

// Endpoint de métricas dos circuit breakers
app.MapCircuitBreakerMetrics();

// Endpoint de teste dos circuit breakers
app.MapGet("/test/circuit-breaker/{processor}", (string processor, ICircuitBreakerFactory factory) =>
{
    try
    {
        var breaker = factory.GetCircuitBreaker(processor);
        var canExecute = breaker.CanExecute();
        var metrics = breaker.GetMetrics();

        return Results.Ok(new
        {
            Processor = processor,
            CanExecute = canExecute,
            Metrics = metrics
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
});

// TODO: Implementar endpoints obrigatorios
// app.MapPost("/payments", async (PaymentRequest request, IDatabaseService db) => { });
// app.MapGet("/payments-summary", async (DateTime? from, DateTime? to, IDatabaseService db) => { });

app.Run();
