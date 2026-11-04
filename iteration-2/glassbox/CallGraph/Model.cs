namespace Glassbox.CallGraph;

public sealed record SourceUnit(string Library, string RelativePath, string Path, string Text);

public sealed record GraphNode(string Id, string Library, string File, int Line, int EndLine, int Ordinal, int Size, string Kind, int Depth, bool Entry, bool Dispatched, bool Instrumentable, IReadOnlyList<string> Operations);

public sealed record GraphEdge(string From, string To, bool Dispatch);

public sealed record BoundaryCall(string From, string To);

public sealed record CallGraph(
    IReadOnlyList<GraphNode> Nodes,
    IReadOnlyList<GraphEdge> Edges,
    IReadOnlyList<BoundaryCall> Boundary,
    IReadOnlyList<string> UnmatchedEntries);

public sealed record EntryOperation(string Name, IReadOnlyList<string> Entries);

public sealed record Reachability(
    Dictionary<string, int> Depth,
    HashSet<GraphEdge> Edges,
    HashSet<BoundaryCall> Boundary,
    HashSet<string> Dispatched,
    HashSet<string> EntryIds,
    List<string> Unmatched);
