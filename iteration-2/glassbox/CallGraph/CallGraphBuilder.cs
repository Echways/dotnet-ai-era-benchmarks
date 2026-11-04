using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Glassbox.CallGraph;

public static class CallGraphBuilder
{
    private static readonly string[] PreprocessorSymbols =
    [
        "NET", "NETCOREAPP", "RELEASE", "TRACE", "IL_EMIT",
        "NETCOREAPP1_0_OR_GREATER", "NETCOREAPP1_1_OR_GREATER", "NETCOREAPP2_0_OR_GREATER", "NETCOREAPP2_1_OR_GREATER",
        "NETCOREAPP2_2_OR_GREATER", "NETCOREAPP3_0_OR_GREATER", "NETCOREAPP3_1_OR_GREATER",
        "NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER", "NET8_0_OR_GREATER", "NET9_0_OR_GREATER", "NET10_0_OR_GREATER", "NET10_0",
    ];

    public static IReadOnlyList<MetadataReference> PlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(path) is "mscorlib.dll" or "netstandard.dll")
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToArray();

    public static CallGraph Build(
        IReadOnlyList<SourceUnit> sources,
        IReadOnlyList<EntryOperation> operations,
        string library,
        IReadOnlyList<MetadataReference> references)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: PreprocessorSymbols);
        var units = sources.ToDictionary(
            unit => CSharpSyntaxTree.ParseText(unit.Text, parseOptions, unit.Path),
            unit => unit);
        var compilation = CSharpCompilation.Create(
            "CallGraph",
            units.Keys,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Disable));
        var index = DeclarationIndex.Build(compilation, units);
        var edges = Edges(index);
        var reaches = operations.Select(operation => (Operation: operation, Reach: Reach(index, edges, operation.Entries, library))).ToArray();
        return Merge(index, reaches);
    }

    public static CallGraph Build(IReadOnlyList<SourceUnit> sources, IReadOnlyList<string> entries, IReadOnlyList<MetadataReference> references) =>
        Build(sources, [new EntryOperation("operation", entries)], sources[0].Library, references);

    private static Dictionary<string, List<(string To, bool Dispatch)>> Edges(DeclarationIndex index)
    {
        var edges = new Dictionary<string, List<(string To, bool Dispatch)>>(StringComparer.Ordinal);
        foreach (var declaration in index.ById.Values)
        {
            if (declaration.Node is null || declaration.Model is null)
            {
                continue;
            }

            Add(edges, declaration.Id, declaration.Node, declaration.Model);
        }

        foreach (var (type, model, _, syntax) in index.Types)
        {
            AddInitializers(index, edges, type, model, syntax);
        }

        return edges;
    }

    private static void Add(Dictionary<string, List<(string To, bool Dispatch)>> edges, string from, SyntaxNode body, SemanticModel model)
    {
        if (!edges.TryGetValue(from, out var list))
        {
            edges[from] = list = [];
        }

        foreach (var target in TargetCollector.Collect(body, model))
        {
            list.Add((Declaration.IdOf(Declaration.Normalize(target)), false));
        }
    }

    private static void AddInitializers(DeclarationIndex index, Dictionary<string, List<(string To, bool Dispatch)>> edges, INamedTypeSymbol type, SemanticModel model, TypeDeclarationSyntax syntax)
    {
        var instance = new List<SyntaxNode>();
        var @static = new List<SyntaxNode>();
        foreach (var member in syntax.Members)
        {
            var isStatic = member.Modifiers.Any(SyntaxKind.StaticKeyword);
            SyntaxNode? initializer = member switch
            {
                FieldDeclarationSyntax field => field.Declaration,
                PropertyDeclarationSyntax { Initializer: not null } property => property.Initializer,
                _ => null,
            };
            if (initializer is not null)
            {
                (isStatic ? @static : instance).Add(initializer);
            }
        }

        var constructors = type.OriginalDefinition.Constructors.Where(c => !c.IsStatic).Select(Declaration.IdOf).Where(index.ById.ContainsKey).ToArray();
        foreach (var node in instance)
        {
            foreach (var constructor in constructors)
            {
                Add(edges, constructor, node, model);
            }
        }

        var staticConstructor = type.OriginalDefinition.StaticConstructors.Select(Declaration.IdOf).FirstOrDefault(index.ById.ContainsKey);
        if (staticConstructor is not null)
        {
            foreach (var node in @static)
            {
                Add(edges, staticConstructor, node, model);
            }
        }
    }

    private static Reachability Reach(DeclarationIndex index, Dictionary<string, List<(string To, bool Dispatch)>> edges, IReadOnlyList<string> entries, string library)
    {
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        var dispatched = new HashSet<string>(StringComparer.Ordinal);
        var entryIds = new HashSet<string>(StringComparer.Ordinal);
        var unmatched = new List<string>();
        var queue = new Queue<string>();
        foreach (var entry in entries)
        {
            var matches = MatchEntry(index, entry, library);
            if (matches.Count == 0)
            {
                unmatched.Add(entry);
            }

            foreach (var id in matches)
            {
                entryIds.Add(id);
                if (depth.TryAdd(id, 0))
                {
                    queue.Enqueue(id);
                }
            }
        }

        var reached = new HashSet<GraphEdge>();
        var boundary = new HashSet<BoundaryCall>();
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var (target, _) in edges.GetValueOrDefault(current) ?? [])
            {
                var implementations = index.ImplementationsOf(target).Where(id => InPackage(index, id, library)).ToArray();
                var direct = InPackage(index, target, library);
                if (!direct && implementations.Length == 0)
                {
                    boundary.Add(new BoundaryCall(current, target));
                    continue;
                }

                if (direct)
                {
                    Visit(current, target, false);
                }

                foreach (var implementation in implementations)
                {
                    dispatched.Add(implementation);
                    Visit(current, implementation, true);
                }
            }

            void Visit(string from, string to, bool dispatch)
            {
                reached.Add(new GraphEdge(from, to, dispatch));
                if (depth.TryAdd(to, depth[from] + 1))
                {
                    queue.Enqueue(to);
                }
            }
        }

        return new Reachability(depth, reached, boundary, dispatched, entryIds, unmatched);
    }

    private static bool InPackage(DeclarationIndex index, string id, string library) =>
        index.ById.TryGetValue(id, out var declaration) && declaration.Unit?.Library == library;

    private static CallGraph Merge(DeclarationIndex index, (EntryOperation Operation, Reachability Reach)[] reaches)
    {
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);
        var operationsOf = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var (operation, reach) in reaches)
        {
            foreach (var (id, nodeDepth) in reach.Depth)
            {
                depth[id] = depth.TryGetValue(id, out var known) ? Math.Min(known, nodeDepth) : nodeDepth;
                if (!operationsOf.TryGetValue(id, out var names))
                {
                    operationsOf[id] = names = new SortedSet<string>(StringComparer.Ordinal);
                }

                names.Add(operation.Name);
            }
        }

        var dispatched = reaches.SelectMany(r => r.Reach.Dispatched).ToHashSet(StringComparer.Ordinal);
        var entryIds = reaches.SelectMany(r => r.Reach.EntryIds).ToHashSet(StringComparer.Ordinal);
        var nodes = depth
            .Select(pair => (Declaration: index.ById[pair.Key], Depth: pair.Value))
            .Select(item => new GraphNode(
                item.Declaration.Id,
                item.Declaration.Unit?.Library ?? string.Empty,
                item.Declaration.Unit?.RelativePath ?? string.Empty,
                item.Declaration.Site.Line,
                item.Declaration.EndLine,
                item.Declaration.Site.Ordinal,
                item.Declaration.Size,
                item.Declaration.Kind,
                item.Depth,
                entryIds.Contains(item.Declaration.Id),
                dispatched.Contains(item.Declaration.Id),
                item.Declaration.Instrumentable,
                operationsOf[item.Declaration.Id].ToArray()))
            .OrderBy(node => node.Depth)
            .ThenBy(node => node.Id, StringComparer.Ordinal)
            .ToArray();
        return new CallGraph(
            nodes,
            reaches.SelectMany(r => r.Reach.Edges).Distinct().OrderBy(e => e.From, StringComparer.Ordinal).ThenBy(e => e.To, StringComparer.Ordinal).ToArray(),
            reaches.SelectMany(r => r.Reach.Boundary).Distinct().OrderBy(b => b.From, StringComparer.Ordinal).ThenBy(b => b.To, StringComparer.Ordinal).ToArray(),
            reaches.SelectMany(r => r.Reach.Unmatched.Select(entry => $"{r.Operation.Name}: {entry}")).ToArray());
    }

    private static List<string> MatchEntry(DeclarationIndex index, string entry, string library)
    {
        if (entry.StartsWith("M:", StringComparison.Ordinal))
        {
            return InPackage(index, entry, library) ? [entry] : [];
        }

        var separator = entry.LastIndexOf('.');
        if (separator < 0)
        {
            return [];
        }

        var typeName = entry[..separator];
        var methodName = entry[(separator + 1)..];
        if (methodName == "#ctor")
        {
            methodName = ".ctor";
        }

        return index.ById.Values
            .Where(d => d.Unit?.Library == library)
            .Where(d => d.Symbol.ContainingType is { } owner && (owner.Name == typeName || owner.ToDisplayString() == typeName) && d.Symbol.MetadataName == methodName)
            .Select(d => d.Id)
            .ToList();
    }
}
