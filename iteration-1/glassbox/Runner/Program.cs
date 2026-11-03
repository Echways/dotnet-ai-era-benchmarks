using System.CommandLine;
using System.Reflection;
using EmberTrace;
using EmberTrace.Sessions;
using Harness.Core;
using Harness.Scenarios;

var methods = new Option<FileInfo>("--methods") { Description = "glassbox/methods.txt", DefaultValueFactory = _ => new FileInfo(Path.Combine("glassbox", "methods.txt")) };
var iterations = new Option<int>("--iterations") { Description = "Measured iterations per scenario", DefaultValueFactory = _ => 5 };
var output = new Option<DirectoryInfo>("--out") { Description = "Output directory", DefaultValueFactory = _ => new DirectoryInfo(Path.Combine("results", "glassbox")) };
var top = new Option<int>("--top") { Description = "Hotspots to print", DefaultValueFactory = _ => 20 };
var root = new RootCommand("Run the DI and Caching.Memory scenarios against an instrumented glass-box build") { methods, iterations, output, top };
root.SetAction(parse =>
{
    var tag = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "GlassboxTag").Value!;
    foreach (var name in File.ReadAllLines(parse.GetValue(methods)!.FullName).Select(l => l.Trim()).Where(l => l.Length > 0))
    {
        Tracer.Id(name);
    }

    var directory = Directory.CreateDirectory(Path.Combine(parse.GetValue(output)!.FullName, tag));
    IScenario[] scenarios = [new DiScenario(), new CacheScenario()];
    foreach (var scenario in scenarios)
    {
        using (scenario)
        {
            var recorder = new Recorder { Recording = true };
            Tracer.Start(new SessionOptions { RuntimeCounters = RuntimeCounters.Gc | RuntimeCounters.Memory, RuntimeCounterInterval = TimeSpan.FromMilliseconds(20), EnableRuntimeMetadata = true });
            scenario.Cold(recorder);
            for (var i = 0; i < parse.GetValue(iterations); i++)
            {
                scenario.Iteration(recorder);
            }

            var session = Tracer.Stop();
            var meta = Tracer.CreateMetadata();
            var processed = session.Process();
            var report = TraceText.Write(processed, meta: meta, topHotspots: parse.GetValue(top), maxDepth: 8);
            var stem = Path.Combine(directory.FullName, scenario.Name);
            TraceFormat.Write(session, stem + ".ember");
            File.WriteAllText(stem + ".hotspots.txt", report);
            using (var folded = new StreamWriter(stem + ".folded"))
            {
                TraceText.WriteCollapsedStacks(processed, folded, meta);
            }

            Console.WriteLine($"===== {tag} {scenario.Name}: {session.EventCount} events, dropped {session.DroppedEvents} =====");
            Console.WriteLine(report);
        }
    }

    var loaded = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).Where(n => n!.StartsWith("Glassbox.V", StringComparison.Ordinal)).Order();
    Console.WriteLine($"[glassbox] {tag}: instrumented assemblies {string.Join(", ", loaded)}");
    return 0;
});
return root.Parse(args).Invoke();
