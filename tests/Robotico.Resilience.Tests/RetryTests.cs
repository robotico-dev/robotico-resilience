using Robotico.Resilience;
using Xunit;

namespace Robotico.Resilience.Tests;

public sealed class RetryTests
{
    [Fact]
    public async Task ExecuteAsync_returns_success_when_operation_succeeds()
    {
        Robotico.Result.Result r = await Retry.ExecuteAsync(() => Task.FromResult(Robotico.Result.Result.Success()));
        Assert.True(r.IsSuccess());
    }
}
