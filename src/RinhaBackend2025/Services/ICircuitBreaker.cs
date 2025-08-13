using RinhaBackend2025.Models;
using System.Runtime.CompilerServices;

namespace RinhaBackend2025.Services;

/// <summary>
/// Interface para circuit breaker ultra-rápido
/// Target: <0.1ms overhead por call
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Verifica se pode fazer requisição (inline para performance)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool CanExecute();

    /// <summary>
    /// Registra sucesso na execução
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void RecordSuccess();

    /// <summary>
    /// Registra falha na execução
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void RecordFailure();

    /// <summary>
    /// Obtém métricas atuais (para observabilidade)
    /// </summary>
    CircuitBreakerMetrics GetMetrics();

    /// <summary>
    /// Nome do processador
    /// </summary>
    string ProcessorName { get; }
}
