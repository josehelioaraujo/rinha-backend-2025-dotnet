namespace RinhaBackend2025.Services;

/// <summary>
/// Factory para criar circuit breakers por processador
/// </summary>
public interface ICircuitBreakerFactory
{
    ICircuitBreaker GetCircuitBreaker(string processorName);
}

public sealed class CircuitBreakerFactory : ICircuitBreakerFactory
{
    private readonly Dictionary<string, ICircuitBreaker> _circuitBreakers;

    public CircuitBreakerFactory()
    {
        _circuitBreakers = new Dictionary<string, ICircuitBreaker>
        {
            ["default"] = new UltraFastCircuitBreaker("default"),
            ["fallback"] = new UltraFastCircuitBreaker("fallback")
        };
    }

    public ICircuitBreaker GetCircuitBreaker(string processorName)
    {
        return _circuitBreakers.TryGetValue(processorName, out var breaker) 
            ? breaker 
            : throw new ArgumentException($"Circuit breaker não encontrado para: {processorName}");
    }
}
