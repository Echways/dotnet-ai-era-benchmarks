using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Glassbox.Instrumenter;

public sealed class InstrumentationJob(
    string tag,
    DirectoryInfo source,
    IReadOnlyList<string> methods,
    IReadOnlyList<string> libraries,
    IReadOnlyList<FileInfo> scenarios,
    DirectoryInfo output)
{
    private static readonly string[] PreprocessorSymbols =
    [
        "NET", "NETCOREAPP", "RELEASE", "TRACE", "IL_EMIT",
        "NETCOREAPP1_0_OR_GREATER", "NETCOREAPP1_1_OR_GREATER", "NETCOREAPP2_0_OR_GREATER", "NETCOREAPP2_1_OR_GREATER",
        "NETCOREAPP2_2_OR_GREATER", "NETCOREAPP3_0_OR_GREATER", "NETCOREAPP3_1_OR_GREATER",
        "NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER", "NET10_0_OR_GREATER", "NET10_0",
    ];

    public int Run()
    {
        var naming = new GlassboxNaming(tag);
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: PreprocessorSymbols);
        var targets = methods.ToHashSet(StringComparer.Ordinal);
        var hits = targets.ToDictionary(t => t, _ => 0, StringComparer.Ordinal);
        var warnings = new List<string>();
        if (output.Exists)
        {
            output.Delete(recursive: true);
        }

        output.Create();
        var built = new List<string>();
        foreach (var library in libraries)
        {
            var selection = SourceSelector.Select(source, library);
            var shortName = naming.ShortName(library);
            var libraryOut = Directory.CreateDirectory(Path.Combine(output.FullName, shortName));
            foreach (var file in selection.Files)
            {
                var text = naming.Rename(File.ReadAllText(file.Path));
                var tree = CSharpSyntaxTree.ParseText(text, parseOptions, file.Path);
                var injector = new ScopeInjector(targets, hits, warnings, tag);
                var rewritten = injector.Visit(tree.GetRoot());
                var destination = Path.Combine(libraryOut.FullName, "src", file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllText(destination, rewritten.ToFullString());
            }

            if (selection.Resources is { } resx)
            {
                var logicalName = naming.AssemblyName(library) + ".Strings";
                File.Copy(resx.FullName, Path.Combine(libraryOut.FullName, "Strings.resx"));
                File.WriteAllText(Path.Combine(libraryOut.FullName, "src", "SR.g.cs"), ResourceGenerator.Generate(resx, logicalName));
            }

            ProjectWriter.Write(libraryOut, naming, library, libraries, selection.Resources is not null, selection.AllowUnsafe);
            built.Add($"{library}: {selection.Files.Count} files ({selection.CommonFiles} shared), resources={selection.Resources is not null}");
        }

        var scenarioOut = Directory.CreateDirectory(Path.Combine(output.FullName, "scenarios"));
        foreach (var scenario in scenarios)
        {
            File.WriteAllText(Path.Combine(scenarioOut.FullName, scenario.Name), naming.Rename(File.ReadAllText(scenario.FullName)));
        }

        foreach (var (target, count) in hits.OrderBy(h => h.Key, StringComparer.Ordinal))
        {
            if (count == 0)
            {
                warnings.Add($"warning: {target} not found in {tag}");
            }
        }

        var report = new List<string> { $"tag: {tag}", "libraries:" };
        report.AddRange(built.Select(b => "  " + b));
        report.Add("instrumented methods (declarations per Type.Method):");
        report.AddRange(hits.OrderBy(h => h.Key, StringComparer.Ordinal).Select(h => $"  {h.Key}: {h.Value}"));
        report.AddRange(warnings);
        File.WriteAllLines(Path.Combine(output.FullName, "instrumentation-report.txt"), report);
        foreach (var line in report)
        {
            Console.WriteLine(line);
        }

        return 0;
    }
}
