namespace Robotico.Resilience;

/// <summary>
/// Retry execution of an operation that returns a Result.
/// </summary>
public static class Retry
{
    /// <summary>
    /// Executes the async operation once and returns its Result. (Minimal implementation; extend for actual retry policy.)
    /// </summary>
    public static async Task<Robotico.Result.Result> ExecuteAsync(Func<Task<Robotico.Result.Result>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        return await operation().ConfigureAwait(false);
    }
}
