using RinhaBackend2025.Services;

namespace RinhaBackend2025.Extensions;

/// <summary>
/// Extensões para configuração do database
/// </summary>
public static class DatabaseExtensions
{
    /// <summary>
    /// Adiciona database service otimizado
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = GetConnectionString(configuration);
        
        services.AddSingleton<IDatabaseService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<DatabaseService>>();
            return new DatabaseService(connectionString, logger);
        });

        return services;
    }

    /// <summary>
    /// Inicializa database na startup
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var databaseService = scope.ServiceProvider.GetRequiredService<IDatabaseService>();
        await databaseService.InitializeAsync();
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        // Caminho para o database SQLite
        var dataDirectory = configuration["DatabasePath"] ?? "/app/data";
        var dbPath = Path.Combine(dataDirectory, "payments.db");
        
        // Criar diretorio se nao existir
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        return $"Data Source={dbPath};Cache=Shared;";
    }
}
