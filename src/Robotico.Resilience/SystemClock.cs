namespace Robotico.Resilience;

/// <summary>Default clock that returns <see cref="DateTimeOffset.UtcNow"/>.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;
}
