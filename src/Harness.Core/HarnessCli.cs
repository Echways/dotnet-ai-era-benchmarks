using System.CommandLine;
using System.Reflection;

namespace Harness.Core;

public static class HarnessCli
{
    public static int Run(
        string[] args,
        string description,
        IReadOnlyList<ScenarioRegistration> registrations,
        IReadOnlyList<string> families,
        Func<RunOptions, IReadOnlyList<FamilyExpectation>> expectations,
        Assembly entry)
    {
        var names = registrations.Select(r => r.Name).Prepend("all").ToArray();
        var scenario = new Option<string>("--scenario") { Description = "Scenario to run", DefaultValueFactory = _ => "all" };
        scenario.AcceptOnlyFromAmong(names);
        var label = new Option<string>("--label") { Description = "Matrix point label", Required = true };
        var iterations = new Option<int>("--iterations") { Description = "Measured iterations", DefaultValueFactory = _ => 200 };
        var warmup = new Option<int>("--warmup") { Description = "Unrecorded warmup iterations", DefaultValueFactory = _ => 20 };
        var output = new Option<string>("--out") { Description = "Raw results directory", DefaultValueFactory = _ => Path.Combine("results", "raw") };
        var includeNew = new Option<bool>("--include-new") { Description = "Also run scenarios that need APIs missing from older versions" };
        var familyOptions = families.ToDictionary(
            f => f,
            f => new Option<int?>($"--expect-{f}") { Description = $"Override the expected major version of the {f} family" });

        var root = new RootCommand(description) { scenario, label, iterations, warmup, output, includeNew };
        foreach (var option in familyOptions.Values)
        {
            root.Options.Add(option);
        }

        root.SetAction(parse =>
        {
            var embedded = EmbeddedMajors(entry);
            var majors = families.ToDictionary(
                f => f,
                f => parse.GetValue(familyOptions[f]) ?? (embedded.TryGetValue(f, out var value) ? value : null));
            var options = new RunOptions(
                parse.GetValue(scenario)!,
                parse.GetValue(label)!,
                parse.GetValue(iterations),
                parse.GetValue(warmup),
                parse.GetValue(output)!,
                parse.GetValue(includeNew),
                majors);
            return ScenarioRunner.Run(options, registrations, expectations, entry);
        });

        return root.Parse(args).Invoke();
    }

    private static Dictionary<string, int?> EmbeddedMajors(Assembly entry) =>
        entry.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Where(a => a.Key.StartsWith("ExpectedMajor:", StringComparison.Ordinal))
            .ToDictionary(
                a => a.Key["ExpectedMajor:".Length..],
                a => int.TryParse(a.Value, out var major) ? major : (int?)null);
}
