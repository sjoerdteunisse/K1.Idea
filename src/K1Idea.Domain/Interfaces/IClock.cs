namespace K1Idea.Domain.Interfaces;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
