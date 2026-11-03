using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using EmberTrace;
using EmberTrace.Analysis.Stats;
using EmberTrace.Metadata;
using EmberTrace.Sessions;

namespace Compare;

public sealed record GcTotals(int Gen0, int Gen1, int Gen2, double PauseMs, int BatchesWithCollection);

public sealed record ScopeRun(int Id, string Name, BatchTimes Times, long? AllocBytes, GcTotals? Gc);

public sealed class TraceRun
{
    private TraceSession? _session;
    private TraceStats? _stats;

    public required PointInfo Point { get; init; }

    public required string Round { get; init; }

    public required string Scenario { get; init; }

    public required string TracePath { get; init; }

    public required int Pid { get; init; }

    public required IReadOnlyDictionary<int, ScopeRun> Scopes { get; init; }

    public bool HasTrace => File.Exists(TracePath);

    public TraceStats Stats => _stats ??= Session.Analyze();

    public ITraceMetadataProvider Metadata => Session.Metadata;

    private TraceSession Session => _session ??= TraceFormat.Read(TracePath);

    public TraceSession LoadSession() => TraceFormat.Read(TracePath);
}

public static class RunLoader
{
    private const int AggregateSchema = 2;

    public static List<TraceRun> Load(DirectoryInfo raw, MatrixPoints points)
    {
        var runs = new List<TraceRun>();
        if (!raw.Exists)
        {
            return runs;
        }

        foreach (var directory in raw.EnumerateDirectories("*", SearchOption.AllDirectories).OrderBy(d => d.FullName, StringComparer.Ordinal))
        {
            var point = points.Resolve(directory.Name);
            var stems = directory.EnumerateFiles("*.ember").Select(f => Path.GetFileNameWithoutExtension(f.Name))
                .Concat(directory.EnumerateFiles("*.aggregate.json").Select(f => f.Name[..^".aggregate.json".Length]))
                .Distinct()
                .Order(StringComparer.Ordinal);
            foreach (var stem in stems)
            {
                var dash = stem.LastIndexOf('-');
                if (dash <= 0 || !int.TryParse(stem.AsSpan(dash + 1), out var pid))
                {
                    continue;
                }

                var scopes = LoadScopes(directory, stem);
                if (scopes is null)
                {
                    continue;
                }

                runs.Add(new TraceRun
                {
                    Point = point,
                    Round = RoundOf(raw, directory),
                    Scenario = stem[..dash],
                    TracePath = Path.Combine(directory.FullName, stem + ".ember"),
                    Pid = pid,
                    Scopes = scopes,
                });
            }
        }

        return runs;
    }

    public static string RoundOf(DirectoryInfo raw, DirectoryInfo pointDirectory)
    {
        var segments = Path.GetRelativePath(raw.FullName, pointDirectory.FullName).Split(Path.DirectorySeparatorChar);
        return segments.Length > 1 ? segments[0] : "";
    }

    private static Dictionary<int, ScopeRun>? LoadScopes(DirectoryInfo directory, string stem)
    {
        var aggregatePath = Path.Combine(directory.FullName, stem + ".aggregate.json");
        var tracePath = Path.Combine(directory.FullName, stem + ".ember");
        var cached = ReadAggregate(aggregatePath);
        if (cached is not null)
        {
            return cached;
        }

        if (!File.Exists(tracePath))
        {
            return null;
        }

        var session = TraceFormat.Read(tracePath);
        var batchTimes = BatchTimes.Read(session);
        var meta = ReadRunMeta(Path.Combine(directory.FullName, stem + ".run-meta.json"));
        var scopes = new Dictionary<int, ScopeRun>();
        foreach (var row in session.Analyze().ByTotalTimeDesc)
        {
            if (RuntimeCounterIds.IsReserved(row.Id))
            {
                continue;
            }

            var name = session.Metadata.TryGet(row.Id, out var scopeMeta) ? scopeMeta.Name : row.Id.ToString(CultureInfo.InvariantCulture);
            meta.TryGetValue(row.Id, out var recorded);
            scopes[row.Id] = new ScopeRun(row.Id, name, batchTimes[row.Id], recorded?.AllocBytes, recorded?.Gc);
        }

        WriteAggregate(aggregatePath, scopes);
        return scopes;
    }

    private static Dictionary<int, ScopeRun>? ReadAggregate(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        var document = JsonSerializer.Deserialize<AggregateFile>(File.ReadAllText(path));
        return document is { Schema: AggregateSchema } ? document.Scopes.ToDictionary(s => s.Id) : null;
    }

    private static void WriteAggregate(string path, Dictionary<int, ScopeRun> scopes) =>
        File.WriteAllText(path, JsonSerializer.Serialize(new AggregateFile(AggregateSchema, scopes.Values.OrderBy(s => s.Id).ToList())));

    private static Dictionary<int, RecordedScope> ReadRunMeta(string path)
    {
        var result = new Dictionary<int, RecordedScope>();
        if (!File.Exists(path))
        {
            return result;
        }

        var document = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var scope in document["scopes"]!.AsArray())
        {
            var gc = scope!["gc_gen0_total"] is null
                ? null
                : new GcTotals(
                    scope["gc_gen0_total"]!.GetValue<int>(),
                    scope["gc_gen1_total"]!.GetValue<int>(),
                    scope["gc_gen2_total"]!.GetValue<int>(),
                    scope["gc_pause_ms_total"]!.GetValue<double>(),
                    scope["gc_batches_with_collection"]!.GetValue<int>());
            result[scope["scope_id"]!.GetValue<int>()] = new RecordedScope(scope["alloc_bytes_median"]!.GetValue<long>(), gc);
        }

        return result;
    }

    private sealed record RecordedScope(long AllocBytes, GcTotals? Gc);

    private sealed record AggregateFile(int Schema, List<ScopeRun> Scopes);
}
