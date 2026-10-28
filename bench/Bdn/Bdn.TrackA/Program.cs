using System.Reflection;
using BenchmarkDotNet.Running;
using Bdn.TrackA;

VersionCheck.Ensure(typeof(Program).Assembly);
var arguments = args.Contains("--inProcess", StringComparer.OrdinalIgnoreCase) ? args : [.. args, "--inProcess"];
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(arguments);

namespace Bdn.TrackA
{
    public static class VersionCheck
    {
        public static void Ensure(Assembly entry)
        {
            var expected = entry.GetCustomAttributes<AssemblyMetadataAttribute>()
                .Where(a => a.Key.StartsWith("ExpectedMajor:", StringComparison.Ordinal))
                .ToDictionary(a => a.Key["ExpectedMajor:".Length..], a => int.Parse(a.Value!, System.Globalization.CultureInfo.InvariantCulture));
            var families = new Dictionary<string, string[]>
            {
                ["msext"] = ["Microsoft.Extensions."],
                ["ef"] = ["Microsoft.EntityFrameworkCore", "Microsoft.Data.Sqlite"],
            };
            var failures = new List<string>();
            foreach (var reference in entry.GetReferencedAssemblies())
            {
                var family = families.FirstOrDefault(f => f.Value.Any(p => reference.Name!.StartsWith(p, StringComparison.Ordinal)));
                if (family.Key is null || !expected.TryGetValue(family.Key, out var major))
                {
                    continue;
                }

                var loaded = Assembly.Load(reference);
                var informational = loaded.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
                Console.WriteLine($"[assembly] {reference.Name} {informational}");
                if (!informational.StartsWith($"{major}.", StringComparison.Ordinal))
                {
                    failures.Add($"{reference.Name} {informational} (expected major {major})");
                }
            }

            if (failures.Count > 0)
            {
                Console.Error.WriteLine("[guard] mismatch: " + string.Join(", ", failures));
                Environment.Exit(3);
            }
        }
    }
}
