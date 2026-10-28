using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.Caching.Memory;

[assembly: TraceId(CacheScopes.ColdCreateAndSet, "cache.cold.create-and-first-set", "cache")]
[assembly: TraceId(CacheScopes.Set, "cache.set.x10k", "cache")]
[assembly: TraceId(CacheScopes.Hit, "cache.try-get-hit.x10k", "cache")]
[assembly: TraceId(CacheScopes.Miss, "cache.try-get-miss.x10k", "cache")]
[assembly: TraceId(CacheScopes.GetOrCreate, "cache.get-or-create.x10k", "cache")]
[assembly: TraceId(CacheScopes.Compact, "cache.size-limit-compact-half.x1", "cache")]
[assembly: TraceId(CacheScopes.Parallel, "cache.parallel8-hit-miss.per-thread-x10k", "cache")]

namespace Harness.Scenarios;

public static class CacheScopes
{
    public const int ColdCreateAndSet = 1400;
    public const int Set = 1410;
    public const int Hit = 1411;
    public const int Miss = 1412;
    public const int GetOrCreate = 1413;
    public const int Compact = 1414;
    public const int Parallel = 1415;
}

public sealed class CacheScenario : IScenario
{
    private const int Batch = 10_000;
    private const int Workers = 8;

    private static readonly object Payload = new();
    private readonly string[] _keys = Enumerable.Range(0, Batch).Select(i => $"key-{i}").ToArray();
    private readonly string[] _missingKeys = Enumerable.Range(0, Batch).Select(i => $"missing-{i}").ToArray();
    private MemoryCache _populated = null!;

    public string Name => "cache";

    public void Cold(Recorder recorder)
    {
        recorder.Cold(CacheScopes.ColdCreateAndSet, () =>
        {
            _populated = new MemoryCache(new MemoryCacheOptions());
            _populated.Set(_keys[0], Payload);
        });
        foreach (var key in _keys)
        {
            _populated.Set(key, Payload);
        }
    }

    public void Iteration(Recorder recorder)
    {
        var keys = _keys;
        var missing = _missingKeys;
        using (var empty = new MemoryCache(new MemoryCacheOptions()))
        {
            recorder.Measure(CacheScopes.Set, Batch, () =>
            {
                for (var i = 0; i < Batch; i++)
                {
                    empty.Set(keys[i], Payload);
                }
            });
        }

        var cache = _populated;
        recorder.Measure(CacheScopes.Hit, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                cache.TryGetValue(keys[i], out var value);
                Sink.Keep(value);
            }
        });

        recorder.Measure(CacheScopes.Miss, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(cache.TryGetValue(missing[i], out _) ? 1 : 0);
            }
        });

        using (var fresh = new MemoryCache(new MemoryCacheOptions()))
        {
            recorder.Measure(CacheScopes.GetOrCreate, Batch, () =>
            {
                for (var i = 0; i < Batch; i++)
                {
                    Sink.Keep(fresh.GetOrCreate(keys[i], static _ => Payload));
                }
            });
        }

        using (var limited = new MemoryCache(new MemoryCacheOptions { SizeLimit = Batch }))
        {
            var entry = new MemoryCacheEntryOptions { Size = 1 };
            foreach (var key in keys)
            {
                limited.Set(key, Payload, entry);
            }

            recorder.Measure(CacheScopes.Compact, 1, () => limited.Compact(0.5));
        }

        recorder.MeasureParallel(CacheScopes.Parallel, Workers, Batch, worker =>
        {
            for (var i = 0; i < Batch; i++)
            {
                var index = (i * 31 + worker * 977) % Batch;
                var key = (i & 1) == 0 ? keys[index] : missing[index];
                cache.TryGetValue(key, out var value);
                Sink.Keep(value);
            }
        });
    }

    public void Dispose() => _populated.Dispose();
}
