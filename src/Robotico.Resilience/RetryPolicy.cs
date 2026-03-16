using Robotico.Result.Errors;

namespace Robotico.Resilience;

/// <summary>
/// Configures retry behavior for <see cref="Retry"/>.
/// Use <see cref="Create"/> for valid configuration (validates MaxAttempts &gt;= 1).
/// </summary>
public sealed class RetryPolicy
{
    /// <summary>
    /// Maximum number of attempts (first try + retries). Must be at least 1.
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Delay between attempts. If null, no delay is applied.
    /// </summary>
    public TimeSpan? DelayBetweenAttempts { get; init; }

    /// <summary>
    /// Optional callback invoked when a retry is about to occur: (attempt number, last error). Use for logging or metrics.
    /// </summary>
    public Action<int, IError>? OnRetry { get; init; }

    /// <summary>
    /// Creates a policy with the given max attempts, optional delay, and optional retry callback.
    /// </summary>
    /// <param name="maxAttempts">Maximum attempts (first + retries). Must be &gt;= 1.</param>
    /// <param name="delayBetweenAttempts">Optional delay between attempts.</param>
    /// <param name="onRetry">Optional callback invoked before each retry: (attempt, lastError).</param>
    /// <returns>A new <see cref="RetryPolicy"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAttempts"/> is less than 1.</exception>
    public static RetryPolicy Create(int maxAttempts = 3, TimeSpan? delayBetweenAttempts = null, Action<int, IError>? onRetry = null)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "MaxAttempts must be at least 1.");
        }

        return new RetryPolicy
        {
            MaxAttempts = maxAttempts,
            DelayBetweenAttempts = delayBetweenAttempts,
            OnRetry = onRetry
        };
    }
}
