using Robotico.Resilience;
using Robotico.Result.Errors;
using Xunit;

namespace Robotico.Resilience.Tests;

public sealed class CircuitBreakerTests
{
    [Fact]
    public async Task ExecuteAsync_when_closed_invokes_operation_and_returns_result()
    {
        CircuitBreaker cb = new(CircuitBreakerOptions.Create(failureThreshold: 2));
        Assert.Equal(CircuitState.Closed, cb.State);
        Robotico.Result.Result r = await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        Assert.True(r.IsSuccess());
    }

    [Fact]
    public async Task ExecuteAsync_opens_after_failure_threshold()
    {
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 2, breakDuration: TimeSpan.FromMinutes(1));
        CircuitBreaker cb = new(options);
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("1"))));
        Assert.Equal(CircuitState.Closed, cb.State);
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("2"))));
        Assert.Equal(CircuitState.Open, cb.State);

        Robotico.Result.Result r = await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        Assert.False(r.IsSuccess());
        r.IsError(out Robotico.Result.Errors.IError? err);
        Assert.NotNull(err);
        Assert.Equal("CIRCUIT_OPEN", err.Code);
    }

    [Fact]
    public async Task ExecuteAsync_half_open_success_closes_circuit()
    {
        ManualClock clock = new();
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 1, breakDuration: TimeSpan.FromSeconds(1), clock: clock);
        CircuitBreaker cb = new(options);
        await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("fail"))));
        Assert.Equal(CircuitState.Open, cb.State);

        clock.Advance(TimeSpan.FromSeconds(2));
        Robotico.Result.Result r = await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        Assert.True(r.IsSuccess());
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public async Task ExecuteAsync_half_open_failure_reopens_circuit()
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
    public async Task Reset_sets_state_to_closed()
    {
        CircuitBreakerOptions options = CircuitBreakerOptions.Create(failureThreshold: 1);
        CircuitBreaker cb = new(options);
        _ = await cb.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(new SimpleError("x"))));
        Assert.Equal(CircuitState.Open, cb.State);
        cb.Reset();
        Assert.Equal(CircuitState.Closed, cb.State);
    }

    [Fact]
    public async Task ExecuteAsync_throws_on_null_operation()
    {
        CircuitBreaker cb = new();
        await Assert.ThrowsAsync<ArgumentNullException>(() => cb.ExecuteAsync(null!));
    }

    [Fact]
    public void CircuitBreakerOptions_Create_throws_when_threshold_less_than_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CircuitBreakerOptions.Create(0));
    }
}
