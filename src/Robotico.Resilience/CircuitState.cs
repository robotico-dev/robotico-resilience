namespace Robotico.Resilience;

/// <summary>
/// Circuit breaker state. Closed = normal; Open = failing fast; HalfOpen = trial.
/// </summary>
public enum CircuitState
{
    /// <summary>Requests are executed normally.</summary>
    Closed,

    /// <summary>Requests fail immediately without calling the operation.</summary>
    Open,

    /// <summary>One request is allowed through to test recovery.</summary>
    HalfOpen
}
