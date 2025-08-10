using RinhaBackend2025.Models;

var builder = WebApplication.CreateBuilder(args);

// TODO: Add services configuration
// TODO: Add database service
// TODO: Add payment processors
// TODO: Add circuit breakers

var app = builder.Build();

// TODO: Add middleware pipeline

// Minimal health check endpoint
app.MapGet("/", () => "Rinha Backend 2025 - .NET Ultra Performance");

// TODO: Implementar endpoints obrigatorios
// app.MapPost("/payments", async (PaymentRequest request) => { });
// app.MapGet("/payments-summary", async (DateTime? from, DateTime? to) => { });

app.Run();
