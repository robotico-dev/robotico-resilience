namespace Robotico.Resilience;

/// <summary>
/// Circuit breaker for operations that return <see cref="Robotico.Result.Result"/>.
/// Tracks consecutive failures; opens after threshold, then allows one trial after break duration.
/// Thread-safe for typical single-instance usage.
/// </summary>
public sealed class CircuitBreaker
{
    private readonly CircuitBreakerOptions _options;
    private readonly IClock _clock;
    private readonly object _lock = new();
    private int _consecutiveFailures;
    private CircuitState _state = CircuitState.Closed;
    private DateTimeOffset _openedAt;

    /// <summary>
    /// Current state of the circuit.
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// Creates a circuit breaker with the given options.
    /// </summary>
    /// <param name="options">Options (failure threshold, break duration). If null, uses defaults.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when options have FailureThreshold &lt; 1.</exception>
    public CircuitBreaker(CircuitBreakerOptions? options = null)
    {
        _options = options ?? CircuitBreakerOptions.Create();
        _clock = _options.Clock ?? new SystemClock();
        if (_options.FailureThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "FailureThreshold must be at least 1.");
        }
    }

    /// <summary>
    /// Executes the operation through the circuit. If circuit is Open, returns the last error without calling the operation.
    /// If HalfOpen, allows one call; success closes the circuit, failure reopens it.
    /// If Closed, runs the operation; success resets failure count, failure increments it and may open the circuit.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Result from the operation, or a failure when circuit is Open.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    public async Task<Robotico.Result.Result> ExecuteAsync(Func<Task<Robotico.Result.Result>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();

        if (ShouldAllowCall(out Robotico.Result.Errors.IError? openError))
        {
            Robotico.Result.Result result = await operation().ConfigureAwait(false);
            RecordResult(result);
            return result;
        }

        return Robotico.Result.Result.Error(openError!);
    }

    private bool ShouldAllowCall(out Robotico.Result.Errors.IError? openError)
    {
        openError = null;
        lock (_lock)
        {
            if (_state == CircuitState.Closed)
            {
                return true;
            }

            if (_state == CircuitState.HalfOpen)
            {
                return true;
            }

            if (_state == CircuitState.Open)
            {
                if (_clock.GetUtcNow() - _openedAt >= _options.BreakDuration)
                {
                    _state = CircuitState.HalfOpen;
                    _options.OnCircuitStateChange?.Invoke(CircuitState.HalfOpen);
                    return true;
                }

                openError = new Robotico.Result.Errors.SimpleError("Circuit breaker is open.", "CIRCUIT_OPEN", Robotico.Result.Errors.ErrorSeverity.Error);
                return false;
            }
        }

        return true;
    }

    private void RecordResult(Robotico.Result.Result result)
    {
        lock (_lock)
        {
            if (result.IsSuccess())
            {
                _consecutiveFailures = 0;
                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Closed;
                    _options.OnCircuitStateChange?.Invoke(CircuitState.Closed);
                }
            }
            else
            {
                if (_state == CircuitState.HalfOpen)
                {
                    _state = CircuitState.Open;
                    _openedAt = _clock.GetUtcNow();
                    _options.OnCircuitStateChange?.Invoke(CircuitState.Open);
                }
                else
                {
                    _consecutiveFailures++;
                    if (_consecutiveFailures >= _options.FailureThreshold)
                    {
                        _state = CircuitState.Open;
                        _openedAt = _clock.GetUtcNow();
                        _options.OnCircuitStateChange?.Invoke(CircuitState.Open);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Resets the circuit to Closed and clears failure count. Use for manual reset.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            if (_state != CircuitState.Closed)
            {
                _state = CircuitState.Closed;
                _consecutiveFailures = 0;
                _options.OnCircuitStateChange?.Invoke(CircuitState.Closed);
            }
            else
            {
                _consecutiveFailures = 0;
            }
        }
    }
}
