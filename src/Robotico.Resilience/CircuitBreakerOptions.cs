namespace Robotico.Resilience;

/// <summary>
/// Configures circuit breaker behavior.
/// Use <see cref="Create"/> for valid configuration (validates FailureThreshold &gt;= 1).
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit. Must be at least 1.
    /// </summary>
    public int FailureThreshold { get; init; } = 5;

    /// <summary>
    /// Duration to keep the circuit open before allowing a trial (HalfOpen).
    /// </summary>
    public TimeSpan BreakDuration { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Optional callback invoked when the circuit state changes (e.g. Closed → Open, Open → HalfOpen). Use for logging or metrics.
    /// </summary>
    public Action<CircuitState>? OnCircuitStateChange { get; init; }

    /// <summary>
    /// Optional clock for "now". When null, <see cref="SystemClock"/> is used. Inject a test double (e.g. <c>ManualClock</c>) in tests to avoid <c>Task.Delay</c> and flaky timing.
    /// </summary>
    public IClock? Clock { get; init; }

    /// <summary>
    /// Creates options with the given threshold, break duration, optional state-change callback, and optional clock.
    /// </summary>
    /// <param name="failureThreshold">Number of consecutive failures before opening. Must be &gt;= 1.</param>
    /// <param name="breakDuration">Duration the circuit stays open before allowing a trial.</param>
    /// <param name="onCircuitStateChange">Optional callback invoked when state changes.</param>
    /// <param name="clock">Optional clock; when null, <see cref="SystemClock"/> is used.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="failureThreshold"/> is less than 1.</exception>
    public static CircuitBreakerOptions Create(int failureThreshold = 5, TimeSpan? breakDuration = null, Action<CircuitState>? onCircuitStateChange = null, IClock? clock = null)
    {
        if (failureThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(failureThreshold), failureThreshold, "FailureThreshold must be at least 1.");
        }

        return new CircuitBreakerOptions
        {
            FailureThreshold = failureThreshold,
            BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30),
            OnCircuitStateChange = onCircuitStateChange,
            Clock = clock
        };
    }
}
