namespace Robotico.Resilience;

/// <summary>
/// Abstraction for current time. Use for testing (inject a clock that can be advanced without real delays)
/// or for deterministic behavior. When not specified, <see cref="CircuitBreaker"/> uses <see cref="SystemClock"/>.
/// </summary>
public interface IClock
{
    /// <summary>Returns the current time in UTC.</summary>
    DateTimeOffset GetUtcNow();
}
