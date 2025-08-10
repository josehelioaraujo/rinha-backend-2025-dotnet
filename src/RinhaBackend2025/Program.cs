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

// TODO: Add payment processors
// TODO: Add circuit breakers
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

// TODO: Implementar endpoints obrigatorios
// app.MapPost("/payments", async (PaymentRequest request, IDatabaseService db) => { });
// app.MapGet("/payments-summary", async (DateTime? from, DateTime? to, IDatabaseService db) => { });

app.Run();
