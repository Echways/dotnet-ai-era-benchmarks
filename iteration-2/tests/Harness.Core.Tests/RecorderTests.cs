using Harness.Core;
using Xunit;

namespace Harness.Core.Tests;

public class RecorderTests
{
    private const int ScopeId = 7;

    [Fact]
    public void Nothing_is_recorded_unless_recording_is_on()
    {
        var recorder = new Recorder();
        var ran = false;

        recorder.Measure(ScopeId, 10, () => ran = true);

        Assert.True(ran);
        Assert.Empty(recorder.Records);
    }

    [Fact]
    public void A_batch_that_allocates_and_collects_is_recorded_with_its_gc_activity()
    {
        var recorder = new Recorder { Recording = true };

        recorder.Measure(ScopeId, 1, () =>
        {
            Sink.Keep(new byte[1_000_000]);
            GC.Collect(0);
        });

        var record = Assert.Single(recorder.Records);
        var gc = Assert.Single(record.Gc);
        Assert.True(record.AllocatedBytes[0] >= 1_000_000);
        Assert.True(gc.Collected);
        Assert.True(gc.Gen0 >= 1);
    }

    [Fact]
    public void A_batch_without_collections_reports_none()
    {
        var recorder = new Recorder { Recording = true };

        recorder.Measure(ScopeId, 1, () => Sink.Keep(1));

        Assert.False(Assert.Single(Assert.Single(recorder.Records).Gc.Select(g => g)).Collected);
    }

    [Fact]
    public void Stability_needs_two_full_windows_of_samples()
    {
        var recorder = new Recorder { Warming = true };
        for (var i = 0; i < 9; i++)
        {
            recorder.Measure(ScopeId, 1, () => Thread.Sleep(1));
        }

        Assert.False(recorder.IsStable(5, 0.5));
    }

    [Fact]
    public void Without_any_scope_the_recorder_counts_as_stable()
    {
        Assert.True(new Recorder().IsStable(5, 0.03));
    }

    [Fact]
    public void Gc_snapshots_subtract_field_by_field()
    {
        var before = new GcSnapshot(1, 2, 3, TimeSpan.FromMilliseconds(5));
        var after = new GcSnapshot(4, 4, 4, TimeSpan.FromMilliseconds(12));

        Assert.Equal(new GcSnapshot(3, 2, 1, TimeSpan.FromMilliseconds(7)), after - before);
    }
}
