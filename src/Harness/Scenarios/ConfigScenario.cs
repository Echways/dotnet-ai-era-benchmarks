using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Core.Data;
using Harness.Scenarios;
using Microsoft.Extensions.Configuration;

[assembly: TraceId(ConfigScopes.ColdBuild, "config.cold.build-json2000", "config")]
[assembly: TraceId(ConfigScopes.ColdBind, "config.cold.bind-poco50", "config")]
[assembly: TraceId(ConfigScopes.Build, "config.build-json2000.x10", "config")]
[assembly: TraceId(ConfigScopes.Indexer, "config.indexer.x10k", "config")]
[assembly: TraceId(ConfigScopes.DeepSection, "config.get-section-deep.x10k", "config")]
[assembly: TraceId(ConfigScopes.Bind, "config.bind-poco50.x1k", "config")]
[assembly: TraceId(ConfigScopes.Reload, "config.reload-json2000.x10", "config")]

namespace Harness.Scenarios;

public static class ConfigScopes
{
    public const int ColdBuild = 1100;
    public const int ColdBind = 1101;
    public const int Build = 1110;
    public const int Indexer = 1111;
    public const int DeepSection = 1112;
    public const int Bind = 1113;
    public const int Reload = 1114;
}

public sealed class ConfigScenario : IScenario
{
    private const int Batch = 10_000;

    private readonly byte[] _json = ConfigDocument.Create();
    private readonly string _directory = Directory.CreateTempSubdirectory("harness-config-").FullName;
    private readonly string _file;
    private IConfigurationRoot _root = null!;
    private IConfigurationRoot _fileRoot = null!;

    public ConfigScenario()
    {
        _file = Path.Combine(_directory, "settings.json");
        File.WriteAllBytes(_file, _json);
    }

    public string Name => "config";

    public void Cold(Recorder recorder)
    {
        recorder.Cold(ConfigScopes.ColdBuild, () => _root = BuildFromStream());
        recorder.Cold(ConfigScopes.ColdBind, () => Sink.Keep(_root.GetSection("Poco").Get<Poco50>()));
        _fileRoot = new ConfigurationBuilder().AddJsonFile(_file, optional: false, reloadOnChange: false).Build();
    }

    public void Iteration(Recorder recorder)
    {
        recorder.Measure(ConfigScopes.Build, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                Sink.Keep(BuildFromStream());
            }
        });

        var root = _root;
        var keys = ConfigDocument.IndexerKeys;
        recorder.Measure(ConfigScopes.Indexer, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(root[keys[i % keys.Length]]);
            }
        });

        recorder.Measure(ConfigScopes.DeepSection, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Sink.Keep(root.GetSection(ConfigDocument.DeepPath).Value);
            }
        });

        var poco = root.GetSection("Poco");
        recorder.Measure(ConfigScopes.Bind, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                Sink.Keep(poco.Get<Poco50>());
            }
        });

        var fileRoot = _fileRoot;
        recorder.Measure(ConfigScopes.Reload, 10, () =>
        {
            for (var i = 0; i < 10; i++)
            {
                fileRoot.Reload();
            }
        });
    }

    public void Dispose()
    {
        (_fileRoot as IDisposable)?.Dispose();
        (_root as IDisposable)?.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private IConfigurationRoot BuildFromStream()
    {
        using var stream = new MemoryStream(_json, writable: false);
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }
}
