using System.Reflection;

namespace Harness.Core;

public sealed record FamilyExpectation(string Family, string[] Prefixes, int? ExpectedMajor);

public sealed record LoadedAssembly(string Name, string Version, string InformationalVersion, string Family, int? ExpectedMajor, bool Matches);

public static class AssemblyGuard
{
    public const int MismatchExitCode = 3;

    public static IReadOnlyList<LoadedAssembly> Inspect(Assembly root, IReadOnlyList<FamilyExpectation> families)
    {
        LoadClosure(root, families);
        var results = new List<LoadedAssembly>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.GetName().Name, StringComparer.Ordinal))
        {
            var name = assembly.GetName();
            var family = families.FirstOrDefault(f => Matches(name.Name, f));
            if (family is null)
            {
                continue;
            }

            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            var major = ParseMajor(informational) ?? name.Version?.Major;
            var matches = family.ExpectedMajor is null || major == family.ExpectedMajor;
            results.Add(new LoadedAssembly(name.Name ?? "", name.Version?.ToString() ?? "", informational, family.Family, family.ExpectedMajor, matches));
        }

        return results;
    }

    public static bool Report(IReadOnlyList<LoadedAssembly> loaded, TextWriter output)
    {
        foreach (var assembly in loaded)
        {
            var verdict = assembly.Matches ? "ok" : $"MISMATCH expected major {assembly.ExpectedMajor}";
            output.WriteLine($"[assembly] {assembly.Name} version={assembly.Version} informational={assembly.InformationalVersion} {verdict}");
        }

        return loaded.All(a => a.Matches);
    }

    private static void LoadClosure(Assembly root, IReadOnlyList<FamilyExpectation> families)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Stack<Assembly>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var reference in current.GetReferencedAssemblies())
            {
                if (reference.Name is null || !seen.Add(reference.Name))
                {
                    continue;
                }

                if (!families.Any(f => Matches(reference.Name, f)) && !reference.Name.StartsWith("Harness", StringComparison.Ordinal))
                {
                    continue;
                }

                pending.Push(Assembly.Load(reference));
            }
        }
    }

    private static bool Matches(string? name, FamilyExpectation family) =>
        name is not null && family.Prefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal));

    private static int? ParseMajor(string informational)
    {
        var dot = informational.IndexOf('.');
        return dot > 0 && int.TryParse(informational.AsSpan(0, dot), out var major) ? major : null;
    }
}
