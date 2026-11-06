using System.Collections.Immutable;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;

[assembly: TraceId(ImmutableScopes.ColdBuild, "immutable.cold.first-build-10k", "immutable")]
[assembly: TraceId(ImmutableScopes.Build, "immutable.dictionary-build-10k.x10", "immutable")]
[assembly: TraceId(ImmutableScopes.Builder, "immutable.builder-add-10k.x10", "immutable")]
[assembly: TraceId(ImmutableScopes.Lookup, "immutable.dictionary-lookup.x10k", "immutable")]
[assembly: TraceId(ImmutableScopes.Array, "immutable.array-create-builder-add.x10", "immutable")]
[assembly: TraceId(ImmutableScopes.DictionaryMutations, "immutable.dictionary-add-set-item-1k.x10", "immutable")]
[assembly: TraceId(ImmutableScopes.ListSet, "immutable.list-set-sorted-dictionary-add-1k.x10", "immutable")]

namespace Harness.TrackB.Scenarios;

public static class ImmutableScopes
{
    public const int ColdBuild = 2200;
    public const int Build = 2210;
    public const int Builder = 2211;
    public const int Lookup = 2212;
    public const int Array = 2213;
    public const int DictionaryMutations = 2214;
    public const int ListSet = 2215;
}

public sealed class ImmutableScenario : IScenario
{
    private const int Count = 10_000;
    private const int Mutations = 1000;

    private readonly KeyValuePair<string, int>[] _items = Enumerable.Range(0, Count)
        .Select(i => new KeyValuePair<string, int>($"item-{i:00000}", i))
        .ToArray();

    private ImmutableDictionary<string, int> _dictionary = ImmutableDictionary<string, int>.Empty;

    public string Name => "immutable";

    public void Cold(Recorder recorder) =>
        recorder.Cold(ImmutableScopes.ColdBuild, () => _dictionary = ImmutableDictionary.CreateRange(StringComparer.Ordinal, _items));

    public void Iteration(Recorder recorder)
    {
        var items = _items;
        recorder.Measure(ImmutableScopes.Build, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                Sink.Keep(ImmutableDictionary.CreateRange(StringComparer.Ordinal, items));
            }
        });

        recorder.Measure(ImmutableScopes.Builder, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                var builder = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
                foreach (var (key, value) in items)
                {
                    builder.Add(key, value);
                }

                Sink.Keep(builder.ToImmutable());
            }
        });

        var dictionary = _dictionary;
        recorder.Measure(ImmutableScopes.Lookup, Count, () =>
        {
            long sum = 0;
            for (var i = 0; i < Count; i++)
            {
                if (dictionary.TryGetValue(items[i * 7 % Count].Key, out var value))
                {
                    sum += value;
                }
            }

            Sink.Keep(sum);
        });

        var numbers = Enumerable.Range(0, Count).ToArray();
        recorder.Measure(ImmutableScopes.Array, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                Sink.Keep(ImmutableArray.CreateRange(numbers));
                var builder = ImmutableArray.CreateBuilder<int>();
                foreach (var number in numbers)
                {
                    builder.Add(number);
                }

                Sink.Keep(builder.ToImmutable());
                var grown = ImmutableArray<int>.Empty;
                for (var j = 0; j < Mutations; j++)
                {
                    grown = grown.Add(j);
                }

                Sink.Keep(grown);
            }
        });

        recorder.Measure(ImmutableScopes.DictionaryMutations, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                var added = ImmutableDictionary<string, int>.Empty;
                for (var j = 0; j < Mutations; j++)
                {
                    added = added.Add(items[j].Key, j);
                }

                for (var j = 0; j < Mutations; j++)
                {
                    added = added.SetItem(items[j].Key, j + 1);
                }

                Sink.Keep(added);
            }
        });

        recorder.Measure(ImmutableScopes.ListSet, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                var list = ImmutableList<int>.Empty;
                var set = ImmutableHashSet<string>.Empty;
                var sorted = ImmutableSortedDictionary<string, int>.Empty;
                for (var j = 0; j < Mutations; j++)
                {
                    list = list.Add(j);
                    set = set.Add(items[j].Key);
                    sorted = sorted.Add(items[j].Key, j);
                }

                Sink.Keep(list);
                Sink.Keep(set);
                Sink.Keep(sorted);
            }
        });
    }

    public void Dispose()
    {
    }
}
