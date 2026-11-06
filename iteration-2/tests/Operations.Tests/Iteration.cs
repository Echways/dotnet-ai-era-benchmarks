using System.Text.RegularExpressions;

namespace Operations.Tests;

public sealed record OperationKey(string Package, string Operation);

public sealed record ScopeRow(string Scope, string Package, string Operation, string State)
{
    public OperationKey Key => new(Package, Operation);
}

public static partial class Iteration
{
    public static readonly string Root = FindRoot();

    public static IReadOnlyList<ScopeRow> ScopeRows { get; } = ReadTable("docs/scope-operations.tsv")
        .Select(f => new ScopeRow(f[0], f[1], f[2], f[3]))
        .ToList();

    public static IReadOnlyList<OperationKey> EntryOperations { get; } = ReadTable("docs/entry-operations.tsv")
        .Select(f => new OperationKey(f[0], f[1]))
        .ToList();

    public static IReadOnlySet<string> NotTraceablePackages { get; } = ReadTable("selection/not-traceable.tsv")
        .Select(f => f[0])
        .ToHashSet();

    public static IReadOnlySet<string> SourceScopes { get; } = Directory
        .EnumerateFiles(Path.Combine(Root, "src"), "*.cs", SearchOption.AllDirectories)
        .SelectMany(file => TraceIdName().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
        .ToHashSet();

    [GeneratedRegex(@"assembly:\s*TraceId\(\s*\w+\.\w+\s*,\s*""([^""]+)""")]
    private static partial Regex TraceIdName();

    private static IEnumerable<string[]> ReadTable(string relativePath) => File
        .ReadAllLines(Path.Combine(Root, relativePath))
        .Skip(1)
        .Where(line => line.Length > 0)
        .Select(line => line.Split('\t'));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "docs", "entry-operations.tsv")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("docs/entry-operations.tsv was not found above the test output");
    }
}
