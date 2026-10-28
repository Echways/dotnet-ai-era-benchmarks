namespace Harness.Core;

public sealed record RunOptions(
    string Scenario,
    string Label,
    int Iterations,
    int Warmup,
    string OutputDirectory,
    bool IncludeNew,
    IReadOnlyDictionary<string, int?> ExpectedMajors);
