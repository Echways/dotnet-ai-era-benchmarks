using System.CommandLine;
using System.Reflection;
using EmberTrace;
using EmberTrace.Sessions;
using Harness.Core;
using Harness.Scenarios;

const int DroppedEventsExitCode = 5;

var methods = new Option<FileInfo>("--methods") { Description = "glassbox/methods.txt", DefaultValueFactory = _ => new FileInfo(Path.Combine("glassbox", "methods.txt")) };
var iterations = new Option<int>("--iterations") { Description = "Measured iterations per scenario", DefaultValueFactory = _ => 10 };
var warmup = new Option<int>("--warmup") { Description = "Minimum unrecorded warmup iterations", DefaultValueFactory = _ => 10 };
var warmupMax = new Option<int>("--warmup-max") { Description = "Warmup iteration cap when batch times have not stabilised", DefaultValueFactory = _ => 300 };
var counterInterval = new Option<int>("--counter-interval-ms") { Description = "Runtime counter sampling interval in milliseconds, 0 disables the counters", DefaultValueFactory = _ => 20 };
var output = new Option<DirectoryInfo>("--out") { Description = "Output directory", DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("results", "glassbox")) };
var report = new Option<bool>("--report") { Description = "Also write the hotspot report and collapsed stacks of this process" };
var top = new Option<int>("--top") { Description = "Hotspots to print", DefaultValueFactory = _ => 20 };
static SessionOptions SessionFor(int interval) => new()
{
    RuntimeCounters = interval > 0 ? RuntimeCounters.Gc | RuntimeCounters.Memory : default,
    RuntimeCounterInterval = TimeSpan.FromMilliseconds(Math.Max(interval, 1)),
    EnableRuntimeMetadata = true,
};

var root = new RootCommand("Run the DI and Caching.Memory scenarios against an instrumented glass-box build") { methods, iterations, warmup, warmupMax, counterInterval, output, report, top };
root.SetAction(parse =>
{
    var tag = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "GlassboxTag").Value!;
    foreach (var name in File.ReadAllLines(parse.GetValue(methods)!.FullName).Select(l => l.Trim()).Where(l => l.Length > 0))
    {
        Tracer.Id(name);
    }

    var directory = Directory.CreateDirectory(Path.Combine(parse.GetValue(output)!.FullName, tag));
    var interval = parse.GetValue(counterInterval);
    var dropped = false;
    IScenario[] scenarios = [new DiScenario(), new CacheScenario()];
    foreach (var scenario in scenarios)
    {
        using (scenario)
        {
            var recorder = new Recorder();
            var stem = Path.Combine(directory.FullName, $"{scenario.Name}-{Environment.ProcessId}");

            Tracer.Start(SessionFor(interval));
            recorder.Recording = true;
            scenario.Cold(recorder);
            recorder.Recording = false;
            var cold = Tracer.Stop();
            TraceFormat.Write(cold, stem + ".cold.ember");

            var warmed = Warmup.Run(scenario, recorder, parse.GetValue(warmup), parse.GetValue(warmupMax));

            Tracer.Start(SessionFor(interval));
            recorder.Recording = true;
            for (var i = 0; i < parse.GetValue(iterations); i++)
            {
                scenario.Iteration(recorder);
            }

            var session = Tracer.Stop();
            var meta = Tracer.CreateMetadata();
            TraceFormat.Write(session, stem + ".ember");
            if (parse.GetValue(report))
            {
                var processed = session.Process();
                File.WriteAllText(stem + ".hotspots.txt", TraceText.Write(processed, meta: meta, topHotspots: parse.GetValue(top), maxDepth: 8));
                using var folded = new StreamWriter(stem + ".folded");
                TraceText.WriteCollapsedStacks(processed, folded, meta);
            }

            dropped |= session.WasOverflow || session.DroppedEvents > 0 || cold.WasOverflow || cold.DroppedEvents > 0;
            Console.WriteLine($"[glassbox] {tag} {scenario.Name}: {session.EventCount} events, dropped {session.DroppedEvents}, warmup {warmed.IterationsUsed} (stable={warmed.Stable}) -> {stem}.ember");
        }
    }

    var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).Where(n => n!.StartsWith("Glassbox.V", StringComparison.Ordinal)).Order();
    Console.WriteLine($"[glassbox] {tag}: instrumented assemblies {string.Join(", ", loaded)}");
    if (dropped)
    {
        Console.Error.WriteLine($"[glassbox] {tag}: events were dropped; lower the batch size or the iteration count, results rejected");
        return DroppedEventsExitCode;
    }

    return 0;
});
return root.Parse(args).Invoke();
