namespace RinhaBackend2025.Models;

/// <summary>
/// Estados do circuit breaker
/// Enum de byte para maxima performance
/// </summary>
public enum CircuitBreakerState : byte
{
    /// <summary>
    /// Funcionamento normal - permite requisicoes
    /// </summary>
    Closed = 0,

    /// <summary>
    /// Muitas falhas - bloqueia requisicoes
    /// </summary>
    Open = 1,

    /// <summary>
    /// Teste de recuperacao - permite uma requisicao
    /// </summary>
    HalfOpen = 2
}

/// <summary>
/// Metricas do circuit breaker para observabilidade
/// </summary>
public sealed record CircuitBreakerMetrics
{
    public required string ProcessorName { get; init; }
    public required CircuitBreakerState State { get; init; }
    public required int FailureCount { get; init; }
    public required DateTime LastFailureTime { get; init; }
    public required int SuccessCount { get; init; }
    public required DateTime LastSuccessTime { get; init; }

    /// <summary>
    /// Indica se esta permitindo requisicoes
    /// </summary>
    public bool IsAllowingRequests => State != CircuitBreakerState.Open;

    /// <summary>
    /// Tempo desde a ultima falha
    /// </summary>
    public TimeSpan TimeSinceLastFailure => DateTime.UtcNow - LastFailureTime;
}
