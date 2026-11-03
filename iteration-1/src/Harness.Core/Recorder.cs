using EmberTrace;

namespace Harness.Core;

public sealed class Recorder
{
    private readonly Dictionary<int, ScopeRecord> _records = new();
    private readonly Lock _gate = new();

    public bool Recording { get; set; }

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

    private void Measure(int scopeId, int operations, Action body, ScopeKind kind)
    {
        if (!Recording)
        {
            body();
            return;
        }

        var processWide = kind == ScopeKind.ProcessBatch;
        var before = processWide ? GC.GetTotalAllocatedBytes(precise: true) : GC.GetAllocatedBytesForCurrentThread();
        using (Tracer.Scope(scopeId))
        {
            body();
        }

        var allocated = (processWide ? GC.GetTotalAllocatedBytes(precise: true) : GC.GetAllocatedBytesForCurrentThread()) - before;
        lock (_gate)
        {
            if (!_records.TryGetValue(scopeId, out var record))
            {
                record = new ScopeRecord(scopeId, operations, kind);
                _records.Add(scopeId, record);
            }

            record.AllocatedBytes.Add(allocated);
        }
    }
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
}
