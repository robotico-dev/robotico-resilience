namespace Robotico.Resilience.Tests;

/// <summary>Clock for tests: set or advance time without real delays.</summary>
public sealed class ManualClock : IClock
{
    private DateTimeOffset _utcNow;

    /// <summary>Creates a clock starting at the given UTC time (default: 2000-01-01 00:00:00).</summary>
    public ManualClock(DateTimeOffset? utcNow = null)
    {
        _utcNow = utcNow ?? new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    /// <inheritdoc />
    public DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>Sets the current time.</summary>
    public void SetUtcNow(DateTimeOffset value) => _utcNow = value;

    /// <summary>Advances the current time by the given duration.</summary>
    public void Advance(TimeSpan duration) => _utcNow += duration;
}
