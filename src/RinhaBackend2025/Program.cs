var builder = WebApplication.CreateBuilder(args);

// TODO: Add services configuration

var app = builder.Build();

// TODO: Add middleware pipeline

// Minimal health check endpoint
app.MapGet("/", () => "Rinha Backend 2025 - .NET Ultra Performance");

app.Run();
