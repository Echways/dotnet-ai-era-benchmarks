using BenchmarkDotNet.Attributes;
using Harness.Scenarios;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Bdn.TrackA;

[MemoryDiagnoser]
public class LoggingBenchmarks
{
    private const string Template = "User {UserId} performed {Action} in {Elapsed} ms";

    private static readonly Action<ILogger, int, string, double, Exception?> Defined =
        LoggerMessage.Define<int, string, double>(LogLevel.Information, new EventId(7, "Performed"), Template);

    private readonly NoIoLoggerProvider _provider = new();
    private ILoggerFactory _factory = null!;
    private ILogger _logger = null!;

    [GlobalSetup]
    public void Setup()
    {
        _factory = LoggerFactory.Create(builder => builder.AddProvider(_provider).SetMinimumLevel(LogLevel.Information));
        _logger = _factory.CreateLogger("Bdn.Logging");
    }

    [GlobalCleanup]
    public void Cleanup() => _factory.Dispose();

    [Benchmark]
    public void LogInformationEnabled() => _logger.LogInformation(Template, 42, "login", 0.5);

    [Benchmark]
    public void LogDebugDisabled() => _logger.LogDebug(Template, 42, "login", 0.5);

    [Benchmark]
    public void LoggerMessageDefine() => Defined(_logger, 42, "login", 0.5, null);

    [Benchmark]
    public void BeginScope()
    {
        using var scope = _logger.BeginScope("Request {RequestId}", 42);
    }
}

[MemoryDiagnoser]
public class CacheBenchmarks
{
    private const int Count = 10_000;
    private static readonly object Payload = new();
    private readonly string[] _keys = Enumerable.Range(0, Count).Select(i => $"key-{i}").ToArray();
    private MemoryCache _cache = null!;
    private int _cursor;

    [GlobalSetup]
    public void Setup()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        foreach (var key in _keys)
        {
            _cache.Set(key, Payload);
        }
    }

    [GlobalCleanup]
    public void Cleanup() => _cache.Dispose();

    [Benchmark]
    public bool TryGetHit() => _cache.TryGetValue(_keys[_cursor++ % Count], out _);

    [Benchmark]
    public bool TryGetMiss() => _cache.TryGetValue("missing", out _);

    [Benchmark]
    public object Set() => _cache.Set(_keys[_cursor++ % Count], Payload);

    [Benchmark]
    public object? GetOrCreateHit() => _cache.GetOrCreate(_keys[_cursor++ % Count], static _ => Payload);
}
