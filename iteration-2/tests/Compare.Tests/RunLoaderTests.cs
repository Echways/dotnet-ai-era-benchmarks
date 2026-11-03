using Compare;
using EmberTrace;
using EmberTrace.Sessions;
using Xunit;

namespace Compare.Tests;

public class RunLoaderTests : IDisposable
{
    private const long Nanoseconds = 1_000_000_000;
    private const int ScopeId = 11;
    private readonly DirectoryInfo _raw = Directory.CreateTempSubdirectory("compare-tests");

    public void Dispose() => _raw.Delete(recursive: true);

    private static MatrixPoints Points()
    {
        var path = Path.Combine(Path.GetTempPath(), $"versions-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{"baseline":"e2-10","points":[{"label":"e2-10","epoch":"E2"}]}""");
        try
        {
            return MatrixPoints.Load(new FileInfo(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private string WriteTrace(string stem)
    {
        var directory = Directory.CreateDirectory(Path.Combine(_raw.FullName, "e2-10"));
        var events = new List<TraceEventRecord>();
        long clock = 0;
        long sequence = 0;
        foreach (var duration in new long[] { 1_000_000, 2_000_000, 3_000_000 })
        {
            events.Add(new TraceEventRecord(ScopeId, 1, clock, TraceEventKind.Begin, 0, 0, sequence++, 0));
            clock += duration;
            events.Add(new TraceEventRecord(ScopeId, 1, clock, TraceEventKind.End, 0, 0, sequence++, 0));
        }

        var path = Path.Combine(directory.FullName, stem + ".ember");
        TraceFormat.Write(TraceSession.FromEvents(events, 0, clock, Nanoseconds), path);
        return path;
    }

    [Fact]
    public void Loading_a_trace_stores_an_aggregate_next_to_it()
    {
        var trace = WriteTrace("cache-100");

        var runs = RunLoader.Load(_raw, Points());

        Assert.Single(runs);
        Assert.True(File.Exists(Path.ChangeExtension(trace, null) + ".aggregate.json"));
    }

    [Fact]
    public void A_pruned_trace_still_loads_from_its_aggregate_with_the_same_numbers()
    {
        var trace = WriteTrace("cache-100");
        var first = RunLoader.Load(_raw, Points()).Single();
        File.Delete(trace);

        var second = RunLoader.Load(_raw, Points()).Single();

        Assert.False(second.HasTrace);
        Assert.Equal(first.Scopes[ScopeId].Times, second.Scopes[ScopeId].Times);
        Assert.Equal(2.0, second.Scopes[ScopeId].Times.P50Ms, 9);
    }

    [Fact]
    public void A_trace_without_an_aggregate_and_a_stem_without_a_pid_are_skipped()
    {
        WriteTrace("cache");

        Assert.Empty(RunLoader.Load(_raw, Points()));
    }
}
