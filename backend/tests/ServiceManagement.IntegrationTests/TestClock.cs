using ServiceManagement.Domain.Services;

namespace ServiceManagement.IntegrationTests;

public sealed class TestClock : IClock
{
    public TestClock(DateTime utcNow)
    {
        UtcNow = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }

    public DateTime UtcNow { get; set; }

    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}
