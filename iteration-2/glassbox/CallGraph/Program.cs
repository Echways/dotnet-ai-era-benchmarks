using System.CommandLine;
using System.Text.Json;
using Glassbox.CallGraph;
using Glassbox.Instrumenter;

var tag = new Option<string>("--tag") { Description = "tag of the checkout, recorded in the output", Required = true };
var source = new Option<DirectoryInfo>("--source") { Description = "Sparse checkout of the tag", Required = true };
var packages = new Option<FileInfo>("--packages") { Description = "packages.tsv: id, repo, source_dir, tests_dir", Required = true };
var repo = new Option<string>("--repo") { Description = "Repository of the checkout", DefaultValueFactory = _ => "runtime" };
var operations = new Option<FileInfo>("--operations") { Description = "entry-operations.tsv: package, operation, entries, description", Required = true };
var only = new Option<string[]>("--package") { Description = "Limit to these package ids", AllowMultipleArgumentsPerToken = true };
var output = new Option<DirectoryInfo>("--out") { Description = "Output directory; one subdirectory per package", Required = true };
var minimumSize = new Option<int>("--min-size") { Description = "Methods with fewer body syntax nodes are left out of methods.tsv", DefaultValueFactory = _ => 0 };
var root = new RootCommand("Build the static call graph of every selected package from its entry operations")
{
    tag, source, packages, repo, operations, only, output, minimumSize,
};
root.SetAction(parse =>
{
    var checkout = parse.GetValue(source)!;
    var all = PackageSet.ReadPackages(parse.GetValue(packages)!);
    var rows = PackageSet.ReadOperations(parse.GetValue(operations)!);
    var limit = parse.GetValue(only) ?? [];
    var failures = 0;
    foreach (var package in all.Where(p => p.Repo == parse.GetValue(repo) && (limit.Length == 0 || limit.Contains(p.Id))))
    {
        if (!File.Exists(Path.Combine(checkout.FullName, package.SourceDirectory, package.Id + ".csproj")))
        {
            Console.WriteLine($"{package.Id}: not present at {parse.GetValue(tag)}");
            continue;
        }

        var packageOperations = rows.Where(r => r.Package == package.Id).Select(r => r.Operation).ToArray();
        if (packageOperations.Length == 0)
        {
            Console.WriteLine($"{package.Id}: no entry operations");
            continue;
        }

        var units = PackageSet.Closure(checkout, all, package)
            .SelectMany(member => SourceSelector.Select(checkout, member.Id, member.SourceDirectory).Files
                .Select(file => new SourceUnit(member.Id, file.RelativePath, file.Path, File.ReadAllText(file.Path))))
            .GroupBy(unit => unit.RelativePath.StartsWith("Common", StringComparison.Ordinal) ? unit.RelativePath : unit.Path)
            .Select(group => group.First())
            .ToArray();
        var graph = CallGraphBuilder.Build(units, packageOperations, package.Id, CallGraphBuilder.PlatformReferences());
        var directory = Directory.CreateDirectory(Path.Combine(parse.GetValue(output)!.FullName, package.Id));
        var document = new { tag = parse.GetValue(tag), package = package.Id, operations = packageOperations, graph.Nodes, graph.Edges, graph.Boundary, graph.UnmatchedEntries };
        File.WriteAllText(
            Path.Combine(directory.FullName, "callgraph.json"),
            JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        var header = "id\tlibrary\tfile\tline\tend_line\tordinal\tkind\tdepth\tsize\tentry\tdispatched\toperations";
        var lines = graph.Nodes
            .Where(node => node.Instrumentable && node.Size >= parse.GetValue(minimumSize))
            .Select(node => string.Join('\t', node.Id, node.Library, node.File, node.Line, node.EndLine, node.Ordinal, node.Kind, node.Depth, node.Size, node.Entry, node.Dispatched, string.Join(';', node.Operations)));
        File.WriteAllLines(Path.Combine(directory.FullName, "methods.tsv"), lines.Prepend(header));
        Console.WriteLine($"{package.Id}: {graph.Nodes.Count} methods, {graph.Edges.Count} edges, {graph.Boundary.Count} boundary calls, {graph.UnmatchedEntries.Count} unmatched entries");
        foreach (var entry in graph.UnmatchedEntries)
        {
            Console.Error.WriteLine($"  unmatched: {entry}");
        }

        failures += graph.UnmatchedEntries.Count;
    }

    return failures == 0 ? 0 : 3;
});
return root.Parse(args).Invoke();
