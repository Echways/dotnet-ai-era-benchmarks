using Harness.Core;
using Xunit;

namespace Harness.Core.Tests;

public class WarmupTests
{
    private const int ScopeId = 1;

    private sealed class ScriptedScenario(Func<int, int> workMilliseconds) : IScenario
    {
        private int _iteration;

        public string Name => "scripted";

        public void Cold(Recorder recorder)
        {
        }

        public void Iteration(Recorder recorder)
        {
            var work = workMilliseconds(_iteration++);
            recorder.Measure(ScopeId, 1, () => Thread.Sleep(work));
        }

        public void Dispose()
        {
        }
    }

    [Fact]
    public void Steady_batches_stop_the_warmup_at_the_minimum_once_two_windows_exist()
    {
        var recorder = new Recorder();
        var result = Warmup.Run(new ScriptedScenario(_ => 2), recorder, 5, 100);

        Assert.True(result.Stable);
        Assert.Equal(2 * Warmup.StabilityWindow, result.IterationsUsed);
    }

    [Fact]
    public void Batches_that_keep_getting_faster_run_to_the_cap()
    {
        var recorder = new Recorder();
        var result = Warmup.Run(new ScriptedScenario(i => Math.Max(1, 40 - (i * 2))), recorder, 5, 20);

        Assert.False(result.Stable);
        Assert.Equal(20, result.IterationsUsed);
    }

    [Fact]
    public void The_minimum_is_honoured_even_when_batches_are_already_steady()
    {
        var recorder = new Recorder();
        var result = Warmup.Run(new ScriptedScenario(_ => 2), recorder, 40, 100);

        Assert.True(result.Stable);
        Assert.Equal(40, result.IterationsUsed);
    }

    [Fact]
    public void Warmup_leaves_warming_off_and_records_nothing()
    {
        var recorder = new Recorder();
        Warmup.Run(new ScriptedScenario(_ => 1), recorder, 5, 30);

        Assert.False(recorder.Warming);
        Assert.Empty(recorder.Records);
    }
}
