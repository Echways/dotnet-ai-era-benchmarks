using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using BenchmarkDotNet.Attributes;
using Harness.TrackB.Scenarios;

namespace Bdn.TrackB;

[MemoryDiagnoser]
public class JsonBenchmarks
{
    private static readonly JsonSerializerOptions Reflection = new();
    private readonly Forecast _forecast = ForecastData.Create(42);
    private byte[] _small = [];
    private string _smallText = "";
    private byte[] _megabyte = [];

    [GlobalSetup]
    public void Setup()
    {
        InBoxCheck.Ensure();
        _small = JsonSerializer.SerializeToUtf8Bytes(_forecast, ForecastContext.Default.Forecast);
        _smallText = System.Text.Encoding.UTF8.GetString(_small);
        _megabyte = ForecastData.CreateMegabyteArray();
    }

    [Benchmark]
    public byte[] SerializeReflection() => JsonSerializer.SerializeToUtf8Bytes(_forecast, Reflection);

    [Benchmark]
    public Forecast? DeserializeReflection() => JsonSerializer.Deserialize<Forecast>(_small, Reflection);

    [Benchmark]
    public byte[] SerializeSourceGen() => JsonSerializer.SerializeToUtf8Bytes(_forecast, ForecastContext.Default.Forecast);

    [Benchmark]
    public Forecast? DeserializeSourceGen() => JsonSerializer.Deserialize(_small, ForecastContext.Default.Forecast);

    [Benchmark]
    public long Utf8JsonReader1Mb()
    {
        var reader = new Utf8JsonReader(_megabyte);
        long tokens = 0;
        while (reader.Read())
        {
            tokens++;
        }

        return tokens;
    }

    [Benchmark]
    public int JsonDocumentParse1Mb()
    {
        using var document = JsonDocument.Parse(_megabyte);
        return document.RootElement.GetArrayLength();
    }

    [Benchmark]
    public string JsonNodeParseRead() => JsonNode.Parse(_smallText)!["Location"]!["City"]!.GetValue<string>();
}

[MemoryDiagnoser]
public class ChannelsBenchmarks
{
    private const int Items = 100_000;

    [GlobalSetup]
    public void Setup() => InBoxCheck.Ensure();

    [Benchmark(OperationsPerInvoke = Items)]
    public long Bounded1Producer4Consumers()
    {
        var channel = Channel.CreateBounded<int>(new BoundedChannelOptions(1024) { FullMode = BoundedChannelFullMode.Wait, SingleWriter = true });
        var consumers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            long sum = 0;
            while (await channel.Reader.WaitToReadAsync().ConfigureAwait(false))
            {
                while (channel.Reader.TryRead(out var item))
                {
                    sum += item;
                }
            }

            return sum;
        })).ToArray();
        var producer = Task.Run(async () =>
        {
            for (var i = 0; i < Items; i++)
            {
                if (!channel.Writer.TryWrite(i))
                {
                    await channel.Writer.WriteAsync(i).ConfigureAwait(false);
                }
            }

            channel.Writer.Complete();
        });
        producer.GetAwaiter().GetResult();
        return consumers.Sum(c => c.GetAwaiter().GetResult());
    }
}

[MemoryDiagnoser]
public class ImmutableBenchmarks
{
    private const int Count = 10_000;
    private readonly KeyValuePair<string, int>[] _items = Enumerable.Range(0, Count).Select(i => new KeyValuePair<string, int>($"item-{i:00000}", i)).ToArray();
    private ImmutableDictionary<string, int> _dictionary = ImmutableDictionary<string, int>.Empty;
    private int _cursor;

    [GlobalSetup]
    public void Setup()
    {
        InBoxCheck.Ensure();
        _dictionary = ImmutableDictionary.CreateRange(StringComparer.Ordinal, _items);
    }

    [Benchmark]
    public ImmutableDictionary<string, int> Build10k() => ImmutableDictionary.CreateRange(StringComparer.Ordinal, _items);

    [Benchmark]
    public ImmutableDictionary<string, int> BuilderAdd10k()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, int>(StringComparer.Ordinal);
        foreach (var item in _items)
        {
            builder.Add(item.Key, item.Value);
        }

        return builder.ToImmutable();
    }

    [Benchmark]
    public bool Lookup() => _dictionary.TryGetValue(_items[_cursor++ * 7 % Count].Key, out _);
}
