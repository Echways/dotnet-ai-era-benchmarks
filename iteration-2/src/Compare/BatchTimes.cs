using EmberTrace.Sessions;

namespace Compare;

public sealed record BatchTimes(long Count, double TotalMs, double P25Ms, double P50Ms, double P75Ms, double P95Ms)
{
    public double MeanMs => TotalMs / Count;

    public static Dictionary<int, BatchTimes> Read(TraceSession session)
    {
        var ticksByScope = new Dictionary<int, List<long>>();
        foreach (var step in new ScopeReader(session).Read())
        {
            if (step.Kind != ScopeStepKind.Close)
            {
                continue;
            }

            if (!ticksByScope.TryGetValue(step.Id, out var ticks))
            {
                ticks = [];
                ticksByScope[step.Id] = ticks;
            }

            ticks.Add(step.DurationTicks);
        }

        var millisecondsPerTick = 1000.0 / session.TimestampFrequency;
        return ticksByScope.ToDictionary(pair => pair.Key, pair => FromTicks(pair.Value, millisecondsPerTick));
    }

    private static BatchTimes FromTicks(List<long> ticks, double millisecondsPerTick)
    {
        var sorted = ticks.Order().Select(t => t * millisecondsPerTick).ToArray();
        return new BatchTimes(sorted.Length, sorted.Sum(), Percentile(sorted, 0.25), Percentile(sorted, 0.50), Percentile(sorted, 0.75), Percentile(sorted, 0.95));
    }

    private static double Percentile(double[] sorted, double fraction)
    {
        var position = fraction * (sorted.Length - 1);
        var lower = (int)Math.Floor(position);
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + ((sorted[upper] - sorted[lower]) * (position - lower));
    }
}
