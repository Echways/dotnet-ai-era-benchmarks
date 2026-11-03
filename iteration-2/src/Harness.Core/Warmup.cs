namespace Harness.Core;

public sealed record WarmupResult(int IterationsUsed, bool Stable);

public static class Warmup
{
    public const int StabilityWindow = 10;
    public const double StabilityTolerance = 0.03;

    public static WarmupResult Run(IScenario scenario, Recorder recorder, int minimum, int maximum)
    {
        recorder.Warming = true;
        var used = 0;
        var stable = false;
        while (used < Math.Max(minimum, maximum))
        {
            scenario.Iteration(recorder);
            used++;
            if (used >= minimum && recorder.IsStable(StabilityWindow, StabilityTolerance))
            {
                stable = true;
                break;
            }
        }

        recorder.Warming = false;
        return new WarmupResult(used, stable);
    }
}
