using BenchmarkDotNet.Attributes;
using Harness.Core.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Bdn.TrackA;

[MemoryDiagnoser]
public class ConfigBenchmarks
{
    private readonly byte[] _json = ConfigDocument.Create();
    private IConfigurationRoot _root = null!;
    private IConfigurationSection _poco = null!;
    private int _cursor;

    [GlobalSetup]
    public void Setup()
    {
        _root = BuildRoot();
        _poco = _root.GetSection("Poco");
    }

    [Benchmark]
    public IConfigurationRoot BuildJson2000() => BuildRoot();

    [Benchmark]
    public string? Indexer() => _root[ConfigDocument.IndexerKeys[_cursor++ % ConfigDocument.IndexerKeys.Length]];

    [Benchmark]
    public string? GetSectionDeep() => _root.GetSection(ConfigDocument.DeepPath).Value;

    [Benchmark]
    public Poco50? BindPoco50() => _poco.Get<Poco50>();

    private IConfigurationRoot BuildRoot()
    {
        using var stream = new MemoryStream(_json, writable: false);
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }
}

[MemoryDiagnoser]
public class OptionsBenchmarks
{
    private ServiceProvider _provider = null!;
    private IOptions<Poco50> _options = null!;
    private IOptionsMonitor<Poco50> _monitor = null!;

    [GlobalSetup]
    public void Setup()
    {
        using var stream = new MemoryStream(ConfigDocument.Create(), writable: false);
        var configuration = new ConfigurationBuilder().AddJsonStream(stream).Build();
        var services = new ServiceCollection();
        services.Configure<Poco50>(configuration.GetSection("Poco"));
        _provider = services.BuildServiceProvider();
        _options = _provider.GetRequiredService<IOptions<Poco50>>();
        _monitor = _provider.GetRequiredService<IOptionsMonitor<Poco50>>();
        _ = _options.Value;
    }

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    [Benchmark]
    public Poco50 OptionsValue() => _options.Value;

    [Benchmark]
    public Poco50 MonitorCurrentValue() => _monitor.CurrentValue;

    [Benchmark]
    public Poco50 SnapshotNewScope()
    {
        using var scope = _provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<Poco50>>().Value;
    }
}
