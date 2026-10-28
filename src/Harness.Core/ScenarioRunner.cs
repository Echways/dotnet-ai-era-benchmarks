using System.Reflection;
using EmberTrace;
using EmberTrace.Sessions;

namespace Harness.Core;

public static class ScenarioRunner
{
    public const int UnavailableExitCode = 4;

    public static int Run(RunOptions options, IReadOnlyList<ScenarioRegistration> registrations, Func<RunOptions, IReadOnlyList<FamilyExpectation>> families, Assembly entry)
    {
        var expectations = families(options);
        var loaded = AssemblyGuard.Inspect(entry, expectations);
        Console.WriteLine($"[runtime] {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        if (!AssemblyGuard.Report(loaded, Console.Out))
        {
            Console.Error.WriteLine("[guard] loaded assembly major version differs from the requested one; results rejected");
            return AssemblyGuard.MismatchExitCode;
        }

        var selected = Select(options, registrations);
        if (selected.Count == 0)
        {
            Console.Error.WriteLine($"[harness] no scenario matches '{options.Scenario}'");
            return 2;
        }

        var explicitUnavailable = selected.Count == 1 && selected[0].Factory is null;
        var directory = Path.Combine(options.OutputDirectory, options.Label);
        Directory.CreateDirectory(directory);
        foreach (var registration in selected)
        {
            if (registration.Factory is null)
            {
                Console.WriteLine($"[harness] {registration.Name}: skipped, {registration.Unavailable}");
                continue;
            }

            RunOne(options, registration, registration.Factory, loaded, directory);
        }

        return explicitUnavailable ? UnavailableExitCode : 0;
    }

    private static List<ScenarioRegistration> Select(RunOptions options, IReadOnlyList<ScenarioRegistration> registrations)
    {
        var all = string.Equals(options.Scenario, "all", StringComparison.OrdinalIgnoreCase);
        return registrations
            .Where(r => all || string.Equals(r.Name, options.Scenario, StringComparison.OrdinalIgnoreCase))
            .Where(r => !r.NewOnly || options.IncludeNew)
            .ToList();
    }

    private static void RunOne(RunOptions options, ScenarioRegistration registration, Func<IScenario> factory, IReadOnlyList<LoadedAssembly> loaded, string directory)
    {
        var started = DateTimeOffset.UtcNow;
        var recorder = new Recorder();
        Tracer.Start(new SessionOptions
        {
            RuntimeCounters = RuntimeCounters.Gc | RuntimeCounters.Memory,
            RuntimeCounterInterval = TimeSpan.FromMilliseconds(20),
        });

        TraceSession session;
        using (var scenario = factory())
        {
            recorder.Recording = true;
            scenario.Cold(recorder);
            recorder.Recording = false;
            for (var i = 0; i < options.Warmup; i++)
            {
                scenario.Iteration(recorder);
            }

            recorder.Recording = true;
            for (var i = 0; i < options.Iterations; i++)
            {
                scenario.Iteration(recorder);
            }

            recorder.Recording = false;
            session = Tracer.Stop();
        }

        var stem = $"{registration.Name}-{Environment.ProcessId}";
        var tracePath = Path.Combine(directory, stem + ".ember");
        TraceFormat.Write(session, tracePath);
        var metadata = Tracer.CreateMetadata();
        var node = RunMetadata.Build(options, registration.Name, tracePath, loaded, metadata, recorder.Records, session, started);
        RunMetadata.Write(node, Path.Combine(directory, stem + ".run-meta.json"));
        if (session.WasOverflow || session.DroppedEvents > 0)
        {
            Console.Error.WriteLine($"[trace] {registration.Name}: overflow={session.WasOverflow} dropped={session.DroppedEvents}");
        }

        Console.WriteLine($"[harness] {registration.Name}: {recorder.Records.Count} scopes, {session.EventCount} events -> {tracePath}");
    }
}

public sealed record ScenarioRegistration(string Name, Func<IScenario>? Factory, bool NewOnly = false, string? Unavailable = null);
