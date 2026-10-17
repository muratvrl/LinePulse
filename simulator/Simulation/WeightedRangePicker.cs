namespace LinePulse.Simulator.Simulation;

public readonly record struct WeightedRange(double Weight, long MinMilliseconds, long MaxMilliseconds);

public static class WeightedRangePicker
{
    public static long Pick(IReadOnlyList<WeightedRange> ranges)
    {
        var totalWeight = ranges.Sum(r => r.Weight);
        var roll = Random.Shared.NextDouble() * totalWeight;

        var cumulative = 0.0;
        foreach (var range in ranges)
        {
            cumulative += range.Weight;
            if (roll <= cumulative)
                return Random.Shared.NextInt64(range.MinMilliseconds, range.MaxMilliseconds + 1);
        }

        return ranges[^1].MinMilliseconds;
    }
}
