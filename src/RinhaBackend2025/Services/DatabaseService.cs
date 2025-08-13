using Microsoft.Data.Sqlite;
using RinhaBackend2025.Models;
using System.Data;

namespace RinhaBackend2025.Services;

/// <summary>
/// Database service otimizado para SQLite + WAL mode
/// Performance target: sub-millisecond queries
/// </summary>
public sealed class DatabaseService : IDatabaseService
{
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _semaphore;
    private readonly ILogger<DatabaseService> _logger;
    private bool _disposed;

    public DatabaseService(string connectionString, ILogger<DatabaseService> logger)
    {
        _logger = logger;
        _connection = new SqliteConnection(connectionString);
        _semaphore = new SemaphoreSlim(1, 1);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _connection.OpenAsync();
            await ConfigurePerformanceSettingsAsync();
            await CreateSchemaAsync();
            
            _logger.LogInformation("Database inicializado com WAL mode e otimizacoes de performance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar database");
            throw;
        }
    }

    private async Task ConfigurePerformanceSettingsAsync()
    {
        var commands = new[]
        {
            // WAL mode para melhor concorrencia
            "PRAGMA journal_mode = WAL;",
            
            // Sincronizacao otimizada
            "PRAGMA synchronous = NORMAL;",
            
            // Cache grande em memoria
            "PRAGMA cache_size = 10000;",
            
            // Store temporario em memoria
            "PRAGMA temp_store = MEMORY;",
            
            // Memory mapping para performance
            "PRAGMA mmap_size = 268435456;", // 256MB
            
            // Timeout para WAL
            "PRAGMA busy_timeout = 30000;",
            
            // Otimizacoes de escrita
            "PRAGMA wal_autocheckpoint = 1000;",
            
            // Analise de query otimizada
            "PRAGMA optimize;"
        };

        foreach (var commandText in commands)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = commandText;
            await command.ExecuteNonQueryAsync();
        }
    }

    private async Task CreateSchemaAsync()
    {
        var createTableSql = """
            CREATE TABLE IF NOT EXISTS payments (
                correlation_id TEXT PRIMARY KEY,
                amount REAL NOT NULL,
                processor TEXT NOT NULL CHECK (processor IN ('default', 'fallback')),
                requested_at INTEGER NOT NULL,
                processed_at INTEGER NOT NULL
            ) WITHOUT ROWID;
            """;

        var createIndexSql = """
            CREATE INDEX IF NOT EXISTS idx_processed_at 
            ON payments(processed_at);
            
            CREATE INDEX IF NOT EXISTS idx_processor_processed_at 
            ON payments(processor, processed_at);
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = createTableSql;
        await command.ExecuteNonQueryAsync();

        command.CommandText = createIndexSql;
        await command.ExecuteNonQueryAsync();
    }

    public async Task SavePaymentAsync(PaymentRecord payment)
    {
        const string sql = """
            INSERT OR REPLACE INTO payments 
            (correlation_id, amount, processor, requested_at, processed_at)
            VALUES ($correlationId, $amount, $processor, $requestedAt, $processedAt)
            """;

        await _semaphore.WaitAsync();
        try
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            
            command.Parameters.AddWithValue("$correlationId", payment.CorrelationId.ToString());
            command.Parameters.AddWithValue("$amount", payment.Amount);
            command.Parameters.AddWithValue("$processor", payment.Processor);
            command.Parameters.AddWithValue("$requestedAt", payment.RequestedAtTicks);
            command.Parameters.AddWithValue("$processedAt", payment.ProcessedAtTicks);

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PaymentsSummary> GetPaymentsSummaryAsync(DateTime? from, DateTime? to)
    {
        var fromTicks = from?.Ticks ?? 0;
        var toTicks = to?.Ticks ?? DateTime.MaxValue.Ticks;

        const string sql = """
            SELECT 
                processor,
                COUNT(*) as total_requests,
                SUM(amount) as total_amount
            FROM payments 
            WHERE processed_at >= $from AND processed_at <= $to
            GROUP BY processor
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$from", fromTicks);
        command.Parameters.AddWithValue("$to", toTicks);

        var defaultSummary = ProcessorSummary.Empty;
        var fallbackSummary = ProcessorSummary.Empty;

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var processor = reader.GetString("processor");
            var totalRequests = reader.GetInt32("total_requests");
            var totalAmount = reader.GetDecimal("total_amount");

            var summary = ProcessorSummary.Create(totalRequests, totalAmount);

            if (processor == "default")
                defaultSummary = summary;
            else if (processor == "fallback")
                fallbackSummary = summary;
        }

        return new PaymentsSummary
        {
            Default = defaultSummary,
            Fallback = fallbackSummary
        };
    }

    public async Task<bool> PaymentExistsAsync(Guid correlationId)
    {
        const string sql = "SELECT 1 FROM payments WHERE correlation_id = $correlationId LIMIT 1";

        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$correlationId", correlationId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null;
    }

    public async Task<DatabaseStats> GetStatsAsync()
    {
        const string sql = """
            SELECT 
                COUNT(*) as total_payments,
                SUM(CASE WHEN processor = 'default' THEN 1 ELSE 0 END) as default_payments,
                SUM(CASE WHEN processor = 'fallback' THEN 1 ELSE 0 END) as fallback_payments,
                SUM(amount) as total_amount
            FROM payments
            """;

        using var command = _connection.CreateCommand();
        command.CommandText = sql;

        using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new DatabaseStats
            {
                TotalPayments = reader.GetInt32("total_payments"),
                DefaultPayments = reader.GetInt32("default_payments"),
                FallbackPayments = reader.GetInt32("fallback_payments"),
                TotalAmount = reader.IsDBNull("total_amount") ? 0 : reader.GetDecimal("total_amount"),
                DatabaseSizeBytes = 0 // TODO: implementar se necessario
            };
        }

        return new DatabaseStats
        {
            TotalPayments = 0,
            DefaultPayments = 0,
            FallbackPayments = 0,
            TotalAmount = 0,
            DatabaseSizeBytes = 0
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _semaphore?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
