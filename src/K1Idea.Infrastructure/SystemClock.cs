using K1Idea.Domain.Interfaces;

namespace K1Idea.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
