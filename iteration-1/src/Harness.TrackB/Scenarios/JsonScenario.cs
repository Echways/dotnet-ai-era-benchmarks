using System.Text.Json;
using System.Text.Json.Nodes;
using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.TrackB.Scenarios;

[assembly: TraceId(JsonScopes.ColdSerialize, "stj.cold.first-serialize-reflection", "stj")]
[assembly: TraceId(JsonScopes.SerializeReflection, "stj.serialize-reflection.x10k", "stj")]
[assembly: TraceId(JsonScopes.DeserializeReflection, "stj.deserialize-reflection.x10k", "stj")]
[assembly: TraceId(JsonScopes.SerializeSourceGen, "stj.serialize-sourcegen.x10k", "stj")]
[assembly: TraceId(JsonScopes.DeserializeSourceGen, "stj.deserialize-sourcegen.x10k", "stj")]
[assembly: TraceId(JsonScopes.Reader, "stj.utf8jsonreader-1mb.x10", "stj")]
[assembly: TraceId(JsonScopes.Document, "stj.jsondocument-parse-1mb.x10", "stj")]
[assembly: TraceId(JsonScopes.Node, "stj.jsonnode-parse-read.x10k", "stj")]

namespace Harness.TrackB.Scenarios;

public static class JsonScopes
{
    public const int ColdSerialize = 2000;
    public const int SerializeReflection = 2010;
    public const int DeserializeReflection = 2011;
    public const int SerializeSourceGen = 2012;
    public const int DeserializeSourceGen = 2013;
    public const int Reader = 2014;
    public const int Document = 2015;
    public const int Node = 2016;
}

public sealed class JsonScenario : IScenario
{
    private const int Batch = 10_000;

    private static readonly JsonSerializerOptions Reflection = new();
    private readonly Forecast _forecast = ForecastData.Create(42);
    private readonly byte[] _small;
    private readonly string _smallText;
    private readonly byte[] _megabyte = ForecastData.CreateMegabyteArray();

    public JsonScenario()
    {
        _small = JsonSerializer.SerializeToUtf8Bytes(_forecast, ForecastContext.Default.Forecast);
        _smallText = System.Text.Encoding.UTF8.GetString(_small);
    }

    public string Name => "stj";

    public void Cold(Recorder recorder) =>
        recorder.Cold(JsonScopes.ColdSerialize, () => Sink.Keep(JsonSerializer.Serialize(_forecast, Reflection)));

    public void Iteration(Recorder recorder)
    {
        var forecast = _forecast;
        var small = _small;
        recorder.Measure(JsonScopes.SerializeReflection, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(JsonSerializer.SerializeToUtf8Bytes(forecast, Reflection));
            }
        });

        recorder.Measure(JsonScopes.DeserializeReflection, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(JsonSerializer.Deserialize<Forecast>(small, Reflection));
            }
        });

        recorder.Measure(JsonScopes.SerializeSourceGen, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(JsonSerializer.SerializeToUtf8Bytes(forecast, ForecastContext.Default.Forecast));
            }
        });

        recorder.Measure(JsonScopes.DeserializeSourceGen, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(JsonSerializer.Deserialize(small, ForecastContext.Default.Forecast));
            }
        });

        var megabyte = _megabyte;
        recorder.Measure(JsonScopes.Reader, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                Sink.Keep(CountTokens(megabyte));
            }
        });

        recorder.Measure(JsonScopes.Document, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                using var document = JsonDocument.Parse(megabyte);
                Sink.Keep(document.RootElement.GetArrayLength());
            }
        });

        var text = _smallText;
        recorder.Measure(JsonScopes.Node, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                var node = JsonNode.Parse(text)!;
                Sink.Keep(node["Location"]!["City"]!.GetValue<string>());
            }
        });
    }

    public void Dispose()
    {
    }

    private static long CountTokens(byte[] utf8)
    {
        var reader = new Utf8JsonReader(utf8);
        long tokens = 0;
        while (reader.Read())
        {
            tokens++;
        }

        return tokens;
    }
}
