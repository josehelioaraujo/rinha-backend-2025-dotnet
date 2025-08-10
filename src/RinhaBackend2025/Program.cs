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

// Configurar services em ordem de dependência
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddResilience();
builder.Services.AddPaymentProcessorClients(builder.Configuration);
builder.Services.AddPaymentService(); // ← NOVO: Service principal

// TODO: Add channel pipeline para background processing

var app = builder.Build();

// Inicializar database na startup
await app.Services.InitializeDatabaseAsync();

// TODO: Add middleware pipeline

// Health check endpoint
app.MapGet("/", () => "Rinha Backend 2025 - .NET Ultra Performance");

// Endpoints de teste e métricas
app.MapGet("/stats", async (IDatabaseService db) =>
{
    var stats = await db.GetStatsAsync();
    return Results.Ok(stats);
});

app.MapCircuitBreakerMetrics();
app.MapHttpClientTests();

// ★ ENDPOINTS OBRIGATÓRIOS DA COMPETIÇÃO ★
app.MapPaymentEndpoints();

// Endpoints de debug (apenas em desenvolvimento)
if (app.Environment.IsDevelopment())
{
    app.MapPaymentDebugEndpoints();
}

app.Run();
