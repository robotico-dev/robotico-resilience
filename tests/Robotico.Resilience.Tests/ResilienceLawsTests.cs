using Robotico.Resilience;
using Robotico.Result.Errors;
using Xunit;

namespace Robotico.Resilience.Tests;

/// <summary>
/// Tests that Retry and CircuitBreaker obey expected behavioral invariants: success on first try = one call,
/// circuit closed → N failures → open → break duration → half-open → success → closed.
/// </summary>
public sealed class ResilienceLawsTests
{
    // --- Retry invariants ---

    [Fact]
    public async Task Retry_success_on_first_try_invokes_operation_exactly_once()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 5);
        int calls = 0;
        Robotico.Result.Result r = await Retry.ExecuteAsync(
            () =>
            {
                calls++;
                return Task.FromResult(Robotico.Result.Result.Success());
            },
            policy);
        Assert.True(r.IsSuccess());
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Retry_all_failures_returns_last_error_after_max_attempts()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 3);
        int calls = 0;
        Robotico.Result.Result r = await Retry.ExecuteAsync(
            () =>
            {
                calls++;
                return Task.FromResult(Robotico.Result.Result.Error(new SimpleError("attempt " + calls)));
            },
            policy);
        Assert.False(r.IsSuccess());
        Assert.Equal(3, calls);
        r.IsError(out Robotico.Result.Errors.IError? err);
        Assert.NotNull(err);
        Assert.Equal("attempt 3", err.Message);
    }

    [Fact]
    public async Task Retry_success_after_k_failures_invokes_operation_k_plus_one_times()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 5);
        int calls = 0;
        Robotico.Result.Result r = await Retry.ExecuteAsync(
            () =>
            {
                calls++;
                if (calls < 3)
                {
                    return Task.FromResult(Robotico.Result.Result.Error(new SimpleError("fail")));
                }

                return Task.FromResult(Robotico.Result.Result.Success());
            },
            policy);
        Assert.True(r.IsSuccess());
        Assert.Equal(3, calls);
    }

    // --- CircuitBreaker invariants ---

    [Fact]
    public async Task CircuitBreaker_closed_success_resets_failure_count()
    {
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 3, breakDuration: TimeSpan.FromMinutes(1));
        CircuitBreaker cb = new(options);
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("1"))));
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("2"))));
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public async Task CircuitBreaker_open_after_threshold_failures_then_half_open_after_break_duration()
    {
        ManualClock clock = new();
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 2, breakDuration: TimeSpan.FromMinutes(1), clock: clock);
        CircuitBreaker cb = new(options);
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("1"))));
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("2"))));
        Assert.Equal(CircuitState.Open, cb.State);

        clock.Advance(TimeSpan.FromMinutes(2));
        Robotico.Result.Result r = await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        Assert.True(r.IsSuccess());
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public async Task CircuitBreaker_half_open_failure_reopens_circuit()
    {
        ManualClock clock = new();
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 1, breakDuration: TimeSpan.FromSeconds(1), clock: clock);
        CircuitBreaker cb = new(options);
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("1"))));
        Assert.Equal(CircuitState.Open, cb.State);

        clock.Advance(TimeSpan.FromSeconds(2));
        Robotico.Result.Result r = await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("2"))));
        Assert.False(r.IsSuccess());
        Assert.Equal(CircuitState.Open, cb.State);
    }

    [Fact]
    public void RetryPolicy_Create_maxAttempts_one_is_valid()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 1);
        Assert.Equal(1, policy.MaxAttempts);
    }

    [Fact]
    public void CircuitBreakerOptions_Create_threshold_one_is_valid()
    {
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 1);
        Assert.Equal(1, options.FailureThreshold);
    }
}
