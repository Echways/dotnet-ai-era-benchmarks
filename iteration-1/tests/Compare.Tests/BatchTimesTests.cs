using Compare;
using EmberTrace.Sessions;
using Xunit;

namespace Compare.Tests;

public class BatchTimesTests
{
    private const long Nanoseconds = 1_000_000_000;
    private const int ScopeId = 7;

    private static TraceSession SessionWithBatches(params long[] durations)
    {
        var events = new List<TraceEventRecord>();
        long clock = 0;
        long sequence = 0;
        foreach (var duration in durations)
        {
            events.Add(new TraceEventRecord(ScopeId, 1, clock, TraceEventKind.Begin, 0, 0, sequence++, 0));
            clock += duration;
            events.Add(new TraceEventRecord(ScopeId, 1, clock, TraceEventKind.End, 0, 0, sequence++, 0));
        }

        return TraceSession.FromEvents(events, 0, clock, Nanoseconds);
    }

    [Fact]
    public void Median_is_exact_rather_than_a_histogram_bucket_edge()
    {
        var times = BatchTimes.Read(SessionWithBatches(1_000_100, 1_000_300, 1_000_200))[ScopeId];

        Assert.Equal(1.0002, times.P50Ms, 9);
    }

    [Fact]
    public void Median_of_even_count_averages_the_middle_batches()
    {
        var times = BatchTimes.Read(SessionWithBatches(1_000_000, 2_000_000, 3_000_000, 4_000_000))[ScopeId];

        Assert.Equal(2.5, times.P50Ms, 9);
    }

    [Fact]
    public void Quartiles_and_p95_interpolate_between_batches()
    {
        var times = BatchTimes.Read(SessionWithBatches(1_000_000, 2_000_000, 3_000_000, 4_000_000, 5_000_000))[ScopeId];

        Assert.Equal(2.0, times.P25Ms, 9);
        Assert.Equal(4.0, times.P75Ms, 9);
        Assert.Equal(4.8, times.P95Ms, 9);
    }

    [Fact]
    public void Mean_is_total_time_over_batch_count()
    {
        var times = BatchTimes.Read(SessionWithBatches(1_000_000, 1_000_000, 10_000_000))[ScopeId];

        Assert.Equal(3, times.Count);
        Assert.Equal(12.0, times.TotalMs, 9);
        Assert.Equal(4.0, times.MeanMs, 9);
    }

    [Fact]
    public void Single_batch_uses_its_duration_for_every_statistic()
    {
        var times = BatchTimes.Read(SessionWithBatches(6_000_000))[ScopeId];

        Assert.Equal(6.0, times.P25Ms, 9);
        Assert.Equal(6.0, times.P50Ms, 9);
        Assert.Equal(6.0, times.P95Ms, 9);
    }

    [Fact]
    public void Round_is_the_first_directory_under_raw_when_points_are_nested()
    {
        var raw = new DirectoryInfo("/data/rounds");

        Assert.Equal("20261030T043112Z", RunLoader.RoundOf(raw, new DirectoryInfo("/data/rounds/20261030T043112Z/raw/e2-10")));
    }

    [Fact]
    public void Round_is_empty_when_points_sit_directly_under_raw()
    {
        var raw = new DirectoryInfo("/results/raw");

        Assert.Equal("", RunLoader.RoundOf(raw, new DirectoryInfo("/results/raw/e2-10")));
    }
}
