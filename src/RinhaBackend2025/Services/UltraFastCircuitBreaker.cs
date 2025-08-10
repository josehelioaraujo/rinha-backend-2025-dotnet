using RinhaBackend2025.Models;
using System.Runtime.CompilerServices;

namespace RinhaBackend2025.Services;

/// <summary>
/// Circuit breaker nativo ultra-rápido
/// Sem overhead de libraries, otimizado para competição
/// </summary>
public sealed class UltraFastCircuitBreaker : ICircuitBreaker
{
    private const int FAILURE_THRESHOLD = 3;
    private const long RECOVERY_TIMEOUT_TICKS = TimeSpan.TicksPerSecond * 10; // 10 segundos
    
    public string ProcessorName { get; }

    // Campos com Interlocked para thread-safety sem locks
    private volatile int _failureCount;
    private volatile int _successCount;
    private long _lastFailureTimestamp; // Usar Interlocked.Read/Exchange
    private long _lastSuccessTimestamp; // Usar Interlocked.Read/Exchange
    private volatile CircuitBreakerState _state;

    public UltraFastCircuitBreaker(string processorName)
    {
        ProcessorName = processorName;
        _state = CircuitBreakerState.Closed;
        Interlocked.Exchange(ref _lastSuccessTimestamp, DateTime.UtcNow.Ticks);
    }

    /// <summary>
    /// Verifica se pode executar requisição
    /// Implementação inline para zero overhead
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CanExecute()
    {
        // Fast path: se está fechado (normal), pode executar
        if (_state == CircuitBreakerState.Closed)
            return true;

        var currentTicks = DateTime.UtcNow.Ticks;

        // Se está aberto, verifica se já pode tentar recuperar
        if (_state == CircuitBreakerState.Open)
        {
            var lastFailure = Interlocked.Read(ref _lastFailureTimestamp);
            if (currentTicks - lastFailure > RECOVERY_TIMEOUT_TICKS)
            {
                // Transição para half-open
                _state = CircuitBreakerState.HalfOpen;
                return true;
            }
            return false;
        }

        // Half-open: permite uma tentativa
        return _state == CircuitBreakerState.HalfOpen;
    }

    /// <summary>
    /// Registra sucesso com otimização agressiva
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordSuccess()
    {
        Interlocked.Exchange(ref _lastSuccessTimestamp, DateTime.UtcNow.Ticks);
        
        // Incrementa contador de sucesso
        Interlocked.Increment(ref _successCount);

        // Se estava em half-open ou open, volta para closed
        if (_state != CircuitBreakerState.Closed)
        {
            _state = CircuitBreakerState.Closed;
            _failureCount = 0; // Reset failure count
        }
    }

    /// <summary>
    /// Registra falha com verificação de threshold
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RecordFailure()
    {
        Interlocked.Exchange(ref _lastFailureTimestamp, DateTime.UtcNow.Ticks);
        
        var failures = Interlocked.Increment(ref _failureCount);

        // Se ultrapassou threshold, abre o circuito
        if (failures >= FAILURE_THRESHOLD)
        {
            _state = CircuitBreakerState.Open;
        }
    }

    /// <summary>
    /// Obtém métricas para observabilidade (não inline por ser menos crítico)
    /// </summary>
    public CircuitBreakerMetrics GetMetrics()
    {
        return new CircuitBreakerMetrics
        {
            ProcessorName = ProcessorName,
            State = _state,
            FailureCount = _failureCount,
            LastFailureTime = new DateTime(Interlocked.Read(ref _lastFailureTimestamp)),
            SuccessCount = _successCount,
            LastSuccessTime = new DateTime(Interlocked.Read(ref _lastSuccessTimestamp))
        };
    }
}
