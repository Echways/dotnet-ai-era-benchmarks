using EmberTrace;

namespace Harness.Core;

public sealed class Recorder(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<int, ScopeRecord> _records = new();
    private readonly Dictionary<int, List<double>> _warmupSamples = new();
    private readonly Lock _gate = new();

    public bool Recording { get; set; }

    public bool Warming { get; set; }

    public IReadOnlyCollection<ScopeRecord> Records
    {
        get
        {
            lock (_gate)
            {
                return _records.Values.OrderBy(r => r.ScopeId).ToArray();
            }
        }
    }

    public bool IsStable(int window, double tolerance)
    {
        lock (_gate)
        {
            foreach (var samples in _warmupSamples.Values)
            {
                if (samples.Count < 2 * window)
                {
                    return false;
                }

                var latest = Median(samples.GetRange(samples.Count - window, window));
                var previous = Median(samples.GetRange(samples.Count - (2 * window), window));
                if (Math.Abs(latest - previous) > tolerance * previous)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public void Cold(int scopeId, Action body)
    {
        var previous = Recording;
        Recording = true;
        Measure(scopeId, 1, body, ScopeKind.Cold);
        Recording = previous;
    }

    public void Measure(int scopeId, int operations, Action body) =>
        Measure(scopeId, operations, body, ScopeKind.Batch);

    public void MeasureProcessWide(int scopeId, int operations, Action body) =>
        Measure(scopeId, operations, body, ScopeKind.ProcessBatch);

    public void MeasureParallel(int scopeId, int workers, int operationsPerWorker, Action<int> body)
    {
        var options = new ParallelOptions { MaxDegreeOfParallelism = workers };
        Parallel.For(0, workers, options, worker =>
            Measure(scopeId, operationsPerWorker, () => body(worker), ScopeKind.ThreadBatch));
    }

    private static double Median(List<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted[sorted.Length / 2];
    }

    private void Measure(int scopeId, int operations, Action body, ScopeKind kind)
    {
        if (!Recording)
        {
            if (Warming)
            {
                var started = _time.GetTimestamp();
                body();
                AddWarmupSample(scopeId, _time.GetElapsedTime(started).TotalMilliseconds);
            }
            else
            {
                body();
            }

            return;
        }

        var processWide = kind == ScopeKind.ProcessBatch;
        var allocatedBefore = processWide ? GC.GetTotalAllocatedBytes(precise: true) : GC.GetAllocatedBytesForCurrentThread();
        var gcBefore = GcSnapshot.Capture();
        using (Tracer.Scope(scopeId))
        {
            body();
        }

        var gc = GcSnapshot.Capture() - gcBefore;
        var allocated = (processWide ? GC.GetTotalAllocatedBytes(precise: true) : GC.GetAllocatedBytesForCurrentThread()) - allocatedBefore;
        lock (_gate)
        {
            if (!_records.TryGetValue(scopeId, out var record))
            {
                record = new ScopeRecord(scopeId, operations, kind);
                _records.Add(scopeId, record);
            }

            record.AllocatedBytes.Add(allocated);
            record.Gc.Add(gc);
        }
    }

    private void AddWarmupSample(int scopeId, double milliseconds)
    {
        lock (_gate)
        {
            if (!_warmupSamples.TryGetValue(scopeId, out var samples))
            {
                samples = [];
                _warmupSamples.Add(scopeId, samples);
            }

            samples.Add(milliseconds);
        }
    }
}

public readonly record struct GcSnapshot(int Gen0, int Gen1, int Gen2, TimeSpan Pause)
{
    public static GcSnapshot Capture() =>
        new(GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2), GC.GetTotalPauseDuration());

    public static GcSnapshot operator -(GcSnapshot after, GcSnapshot before) =>
        new(after.Gen0 - before.Gen0, after.Gen1 - before.Gen1, after.Gen2 - before.Gen2, after.Pause - before.Pause);

    public bool Collected => Gen0 + Gen1 + Gen2 > 0;
}

public enum ScopeKind
{
    Cold,
    Batch,
    ThreadBatch,
    ProcessBatch,
}

public sealed class ScopeRecord(int scopeId, int operations, ScopeKind kind)
{
    public int ScopeId { get; } = scopeId;

    public int OperationsPerBatch { get; } = operations;

    public ScopeKind Kind { get; } = kind;

    public List<long> AllocatedBytes { get; } = [];

    public List<GcSnapshot> Gc { get; } = [];
}
