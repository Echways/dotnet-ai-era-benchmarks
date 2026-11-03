using System.Globalization;
using System.Text;
using EmberTrace;
using EmberTrace.Metadata;
using EmberTrace.Sessions;

namespace Compare;

public sealed record MethodTimes(int Id, string Name, long Calls, double InclusiveMs, double SelfMs)
{
    public static List<MethodTimes> Read(TraceSession session)
    {
        var totals = new Dictionary<int, Totals>();
        var openByTrackAndId = new Dictionary<(int Track, int Id), int>();
        foreach (var step in new ScopeReader(session).Read())
        {
            var key = (step.TrackId, step.Id);
            if (step.Kind == ScopeStepKind.Open)
            {
                openByTrackAndId.TryGetValue(key, out var open);
                openByTrackAndId[key] = open + 1;
                step.Tag = new Frame(open == 0);
                continue;
            }

            var frame = (Frame)step.Tag!;
            openByTrackAndId[key]--;
            if (!totals.TryGetValue(step.Id, out var total))
            {
                total = new Totals();
                totals[step.Id] = total;
            }

            total.Calls++;
            total.SelfTicks += step.DurationTicks - frame.ChildTicks;
            if (frame.Outermost)
            {
                total.InclusiveTicks += step.DurationTicks;
            }

            if (step.ParentTag is Frame parent)
            {
                parent.ChildTicks += step.DurationTicks;
            }
        }

        var millisecondsPerTick = 1000.0 / session.TimestampFrequency;
        return totals
            .Where(pair => !RuntimeCounterIds.IsReserved(pair.Key))
            .Select(pair => new MethodTimes(pair.Key, NameOf(session.Metadata, pair.Key), pair.Value.Calls, pair.Value.InclusiveTicks * millisecondsPerTick, pair.Value.SelfTicks * millisecondsPerTick))
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ToList();
    }

    public static string CsvHeader => "tag,scenario,pid,method,calls,inclusive_ms,self_ms";

    public string CsvRow(string tag, string scenario, int pid) =>
        string.Create(CultureInfo.InvariantCulture, $"{tag},{scenario},{pid},{Escape(Name)},{Calls},{InclusiveMs:R},{SelfMs:R}");

    public static string WriteCsv(IEnumerable<string> rows) =>
        new StringBuilder().AppendLine(CsvHeader).AppendJoin('\n', rows).AppendLine().ToString();

    private static string NameOf(ITraceMetadataProvider metadata, int id) =>
        metadata.TryGet(id, out var scope) ? scope.Name : id.ToString(CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        value.AsSpan().IndexOfAny(',', '"') < 0 ? value : "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private sealed class Frame(bool outermost)
    {
        public bool Outermost { get; } = outermost;

        public long ChildTicks { get; set; }
    }

    private sealed class Totals
    {
        public long Calls { get; set; }

        public long InclusiveTicks { get; set; }

        public long SelfTicks { get; set; }
    }
}
