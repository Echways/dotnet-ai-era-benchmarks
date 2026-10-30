using System.Text.Json.Nodes;
using EmberTrace;
using EmberTrace.Analysis.Stats;
using EmberTrace.Metadata;
using EmberTrace.Sessions;

namespace Compare;

public sealed record ScopeRun(int Id, string Name, BatchTimes Times, long? AllocBytes);

public sealed class TraceRun
{
    public required PointInfo Point { get; init; }

    public required string Round { get; init; }

    public required string Scenario { get; init; }

    public required string TracePath { get; init; }

    public required int Pid { get; init; }

    public required TraceStats Stats { get; init; }

    public required ITraceMetadataProvider Metadata { get; init; }

    public required IReadOnlyDictionary<int, ScopeRun> Scopes { get; init; }

    public TraceSession LoadSession() => TraceFormat.Read(TracePath);
}

public static class RunLoader
{
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
            foreach (var file in directory.EnumerateFiles("*.ember").OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                var stem = Path.GetFileNameWithoutExtension(file.Name);
                var dash = stem.LastIndexOf('-');
                if (dash <= 0 || !int.TryParse(stem.AsSpan(dash + 1), out var pid))
                {
                    continue;
                }

                var session = TraceFormat.Read(file.FullName);
                var stats = session.Analyze();
                var batchTimes = BatchTimes.Read(session);
                var allocations = ReadAllocations(Path.Combine(directory.FullName, stem + ".run-meta.json"));
                var scopes = new Dictionary<int, ScopeRun>();
                foreach (var row in stats.ByTotalTimeDesc)
                {
                    if (RuntimeCounterIds.IsReserved(row.Id))
                    {
                        continue;
                    }

                    var name = session.Metadata.TryGet(row.Id, out var meta) ? meta.Name : row.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    scopes[row.Id] = new ScopeRun(row.Id, name, batchTimes[row.Id], allocations.TryGetValue(row.Id, out var bytes) ? bytes : null);
                }

                runs.Add(new TraceRun
                {
                    Point = point,
                    Round = RoundOf(raw, directory),
                    Scenario = stem[..dash],
                    TracePath = file.FullName,
                    Pid = pid,
                    Stats = stats,
                    Metadata = session.Metadata,
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

    private static Dictionary<int, long> ReadAllocations(string path)
    {
        var result = new Dictionary<int, long>();
        if (!File.Exists(path))
        {
            return result;
        }

        var document = JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var scope in document["scopes"]!.AsArray())
        {
            result[scope!["scope_id"]!.GetValue<int>()] = scope["alloc_bytes_median"]!.GetValue<long>();
        }

        return result;
    }
}
