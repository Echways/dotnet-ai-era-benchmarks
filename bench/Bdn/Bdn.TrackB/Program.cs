using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Bdn.TrackB;

InBoxCheck.Ensure();
var config = ManualConfig.Create(DefaultConfig.Instance).AddColumn(new TagColumn("Measures", _ => "library + JIT"));
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

namespace Bdn.TrackB
{
    public static class InBoxCheck
    {
        public static void Ensure()
        {
            var runtimeMajor = Environment.Version.Major;
            Type[] probes = [typeof(System.Text.Json.JsonSerializer), typeof(System.Collections.Immutable.ImmutableDictionary), typeof(System.Threading.Channels.Channel), typeof(Microsoft.AspNetCore.Builder.WebApplication)];
            foreach (var probe in probes)
            {
                var name = probe.Assembly.GetName();
                Console.WriteLine($"[inbox] runtime {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}: {name.Name} {name.Version}");
                if (name.Version?.Major != runtimeMajor)
                {
                    Console.Error.WriteLine($"[guard] {name.Name} {name.Version} is not the in-box copy of runtime {runtimeMajor}");
                    Environment.Exit(3);
                }
            }
        }
    }
}
