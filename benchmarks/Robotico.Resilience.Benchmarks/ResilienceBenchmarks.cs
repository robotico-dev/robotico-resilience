using BenchmarkDotNet.Attributes;
using Robotico.Resilience;
using Robotico.Result.Errors;
using VoidResult = Robotico.Result.Result;

namespace Robotico.Resilience.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class ResilienceBenchmarks
{
    private static readonly Task<VoidResult> SuccessTask = Task.FromResult(VoidResult.Success());
    private static readonly Task<VoidResult> ErrorTask = Task.FromResult(VoidResult.Error(new SimpleError("bench")));
    private RetryPolicy _retryPolicy = null!;
    private CircuitBreaker _circuitBreaker = null!;

    [GlobalSetup]
    public void Setup()
    {
        _retryPolicy = RetryPolicy.Create(maxAttempts: 3);
        _circuitBreaker = new CircuitBreaker(CircuitBreakerOptions.Create(failureThreshold: 5, breakDuration: TimeSpan.FromSeconds(30)));
    }

    [Benchmark(Baseline = true)]
    public async Task Retry_Success_FirstTry()
    {
        _ = await Retry.ExecuteAsync(() => SuccessTask, _retryPolicy);
    }

    [Benchmark]
    public async Task Retry_NoPolicy_Success()
    {
        _ = await Retry.ExecuteAsync(() => SuccessTask);
    }

    [Benchmark]
    public async Task CircuitBreaker_Closed_Success()
    {
        _ = await _circuitBreaker.ExecuteAsync(() => SuccessTask);
    }

    [Benchmark]
    public async Task CircuitBreaker_Closed_Error()
    {
        _ = await _circuitBreaker.ExecuteAsync(() => ErrorTask);
    }
}
