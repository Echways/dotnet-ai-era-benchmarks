namespace Harness.Core;

public sealed record RunOptions(
    string Scenario,
    string Label,
    int Iterations,
    int Warmup,
    int WarmupMax,
    int CounterIntervalMilliseconds,
    string OutputDirectory,
    bool IncludeNew,
    IReadOnlyDictionary<string, int?> ExpectedMajors);
