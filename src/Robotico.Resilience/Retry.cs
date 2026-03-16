namespace Robotico.Resilience;

/// <summary>
/// Retry execution of an operation that returns a <see cref="Robotico.Result.Result"/>.
/// Retries only when the result is an error; success is returned immediately.
/// </summary>
public static class Retry
{
    /// <summary>
    /// Executes the async operation once and returns its Result (no retries).
    /// Use <see cref="ExecuteAsync(Func{Task{Robotico.Result.Result}}, RetryPolicy, CancellationToken)"/> for retries.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Result from the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    public static async Task<Robotico.Result.Result> ExecuteAsync(Func<Task<Robotico.Result.Result>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return await operation().ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the async operation with retry policy. Retries only when the result is an error;
    /// after each failed attempt waits <see cref="RetryPolicy.DelayBetweenAttempts"/> if set, then tries again.
    /// Returns the last result (success or error) when max attempts are exhausted or operation succeeds.
    /// </summary>
    /// <param name="operation">The operation to execute (may be called multiple times).</param>
    /// <param name="policy">Retry policy (max attempts, delay). If null, uses default (3 attempts, no delay).</param>
    /// <param name="cancellationToken">Cancellation token (checked before each attempt and during delay).</param>
    /// <returns>The last Result (success or error).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="policy"/> has MaxAttempts &lt; 1.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    public static async Task<Robotico.Result.Result> ExecuteAsync(Func<Task<Robotico.Result.Result>> operation, RetryPolicy? policy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        policy ??= RetryPolicy.Create();
        if (policy.MaxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(policy), "Policy MaxAttempts must be at least 1.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        Robotico.Result.Result lastResult = await operation().ConfigureAwait(false);
        int attempt = 1;
        while (attempt < policy.MaxAttempts && lastResult.IsError(out Robotico.Result.Errors.IError? lastError))
        {
            policy.OnRetry?.Invoke(attempt, lastError);

            cancellationToken.ThrowIfCancellationRequested();
            if (policy.DelayBetweenAttempts.HasValue && policy.DelayBetweenAttempts.Value > TimeSpan.Zero)
            {
                await Task.Delay(policy.DelayBetweenAttempts.Value, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            lastResult = await operation().ConfigureAwait(false);
            attempt++;
        }

        return lastResult;
    }
}
