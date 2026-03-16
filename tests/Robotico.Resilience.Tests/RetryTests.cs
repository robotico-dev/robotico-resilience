using Robotico.Resilience;
using Robotico.Result.Errors;
using Xunit;

namespace Robotico.Resilience.Tests;

public sealed class RetryTests
{
    [Fact]
    public async Task ExecuteAsync_no_policy_returns_success_when_operation_succeeds()
    {
        Robotico.Result.Result r = await Retry.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        Assert.True(r.IsSuccess());
    }

    [Fact]
    public async Task ExecuteAsync_no_policy_returns_error_when_operation_fails()
    {
        SimpleError err = new("fail");
        Robotico.Result.Result r = await Retry.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Error(err)));
        Assert.False(r.IsSuccess());
        r.IsError(out Robotico.Result.Errors.IError? e);
        Assert.Same(err, e);
    }

    [Fact]
    public async Task ExecuteAsync_with_policy_succeeds_on_first_try()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 3);
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
    public async Task ExecuteAsync_with_policy_succeeds_after_failures()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 4);
        int calls = 0;
        Robotico.Result.Result r = await Retry.ExecuteAsync(
            () =>
            {
                calls++;
                if (calls < 3)
                {
                    return Task.FromResult(Robotico.Result.Result.Error(new SimpleError("try " + calls)));
                }

                return Task.FromResult(Robotico.Result.Result.Success());
            },
            policy);
        Assert.True(r.IsSuccess());
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task ExecuteAsync_with_policy_returns_last_error_when_exhausted()
    {
        RetryPolicy policy = RetryPolicy.Create(maxAttempts: 3);
        int calls = 0;
        SimpleError lastError = new("third");
        Robotico.Result.Result r = await Retry.ExecuteAsync(
            () =>
            {
                calls++;
                SimpleError e = new("fail " + calls);
                if (calls == 3)
                {
                    lastError = e;
                }

                return Task.FromResult(Robotico.Result.Result.Error(e));
            },
            policy);
        Assert.False(r.IsSuccess());
        Assert.Equal(3, calls);
        r.IsError(out Robotico.Result.Errors.IError? e);
        Assert.NotNull(e);
        Assert.Equal("fail 3", e.Message);
    }

    [Fact]
    public async Task ExecuteAsync_throws_on_null_operation()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Retry.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_cancellation_before_first_call()
    {
        using CancellationTokenSource cts = new();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            Retry.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()), null, cts.Token));
    }

    [Fact]
    public void RetryPolicy_Create_throws_when_maxAttempts_less_than_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RetryPolicy.Create(0));
    }
}
