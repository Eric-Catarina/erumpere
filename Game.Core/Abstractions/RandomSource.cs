namespace Game.Core.Abstractions;

public interface IRandomSource
{
    int Next(int minValue, int maxValue);
    double NextDouble();
}

public sealed class SeededRandomSource : IRandomSource
{
    private readonly Random _random;

    public SeededRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);

    public double NextDouble() => _random.NextDouble();
}
