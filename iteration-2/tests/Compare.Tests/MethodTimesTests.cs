using EmberTrace.Sessions;
using Xunit;

namespace Compare.Tests;

public class MethodTimesTests
{
    private const long Nanoseconds = 1_000_000_000;
    private const int Outer = 11;
    private const int Inner = 12;

    private static TraceSession Session(params (int Id, TraceEventKind Kind, long At)[] events)
    {
        var records = events.Select((e, i) => new TraceEventRecord(e.Id, 1, e.At, e.Kind, 0, 0, i, 0)).ToList();
        return TraceSession.FromEvents(records, 0, events.Max(e => e.At), Nanoseconds);
    }

    private static MethodTimes Find(List<MethodTimes> methods, int id) => methods.Single(m => m.Id == id);

    [Fact]
    public void Self_time_excludes_children_and_inclusive_includes_them()
    {
        var methods = MethodTimes.Read(Session(
            (Outer, TraceEventKind.Begin, 0),
            (Inner, TraceEventKind.Begin, 1_000_000),
            (Inner, TraceEventKind.End, 4_000_000),
            (Outer, TraceEventKind.End, 10_000_000)));

        Assert.Equal(10.0, Find(methods, Outer).InclusiveMs, 9);
        Assert.Equal(7.0, Find(methods, Outer).SelfMs, 9);
        Assert.Equal(3.0, Find(methods, Inner).InclusiveMs, 9);
        Assert.Equal(3.0, Find(methods, Inner).SelfMs, 9);
    }

    [Fact]
    public void Calls_and_times_accumulate_over_repeated_scopes()
    {
        var methods = MethodTimes.Read(Session(
            (Inner, TraceEventKind.Begin, 0),
            (Inner, TraceEventKind.End, 2_000_000),
            (Inner, TraceEventKind.Begin, 3_000_000),
            (Inner, TraceEventKind.End, 7_000_000)));

        var inner = Find(methods, Inner);
        Assert.Equal(2, inner.Calls);
        Assert.Equal(6.0, inner.InclusiveMs, 9);
        Assert.Equal(6.0, inner.SelfMs, 9);
    }

    [Fact]
    public void Recursion_counts_inclusive_time_once_and_self_time_per_frame()
    {
        var methods = MethodTimes.Read(Session(
            (Outer, TraceEventKind.Begin, 0),
            (Outer, TraceEventKind.Begin, 2_000_000),
            (Outer, TraceEventKind.End, 6_000_000),
            (Outer, TraceEventKind.End, 10_000_000)));

        var outer = Find(methods, Outer);
        Assert.Equal(2, outer.Calls);
        Assert.Equal(10.0, outer.InclusiveMs, 9);
        Assert.Equal(10.0, outer.SelfMs, 9);
    }

    [Fact]
    public void Csv_row_quotes_names_with_commas()
    {
        var row = new MethodTimes(1, "Foo<A,B>.Bar", 3, 1.5, 0.5).CsvRow("v10.0.14", "di", 42);

        Assert.Equal("v10.0.14,di,42,\"Foo<A,B>.Bar\",3,1.5,0.5", row);
    }
}
