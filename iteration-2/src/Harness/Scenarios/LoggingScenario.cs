using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.Logging;

[assembly: TraceId(LoggingScopes.ColdFactory, "logging.cold.factory-create", "logging")]
[assembly: TraceId(LoggingScopes.ColdFirstLog, "logging.cold.first-log", "logging")]
[assembly: TraceId(LoggingScopes.Factory, "logging.factory-create.x100", "logging")]
[assembly: TraceId(LoggingScopes.Enabled, "logging.log-information-3args-enabled.x10k", "logging")]
[assembly: TraceId(LoggingScopes.Disabled, "logging.log-debug-3args-disabled.x10k", "logging")]
[assembly: TraceId(LoggingScopes.Define, "logging.logger-message-define.x10k", "logging")]
[assembly: TraceId(LoggingScopes.BeginScope, "logging.begin-scope.x10k", "logging")]

namespace Harness.Scenarios;

public static class LoggingScopes
{
    public const int ColdFactory = 1300;
    public const int ColdFirstLog = 1301;
    public const int Factory = 1310;
    public const int Enabled = 1311;
    public const int Disabled = 1312;
    public const int Define = 1313;
    public const int BeginScope = 1314;
}

public sealed class LoggingScenario : IScenario
{
    private const int Batch = 10_000;
    private const string Template = "User {UserId} performed {Action} in {Elapsed} ms";

    private static readonly Action<ILogger, int, string, double, Exception?> Defined =
        LoggerMessage.Define<int, string, double>(LogLevel.Information, new EventId(7, "Performed"), Template);

    private readonly NoIoLoggerProvider _provider = new();
    private ILoggerFactory _factory = null!;
    private ILogger _logger = null!;

    public string Name => "logging";

    public void Cold(Recorder recorder)
    {
        recorder.Cold(LoggingScopes.ColdFactory, () =>
        {
            _factory = CreateFactory();
            _logger = _factory.CreateLogger("Harness.Logging");
        });
        recorder.Cold(LoggingScopes.ColdFirstLog, () => _logger.LogInformation(Template, 1, "cold", 0.5));
    }

    public void Iteration(Recorder recorder)
    {
        recorder.Measure(LoggingScopes.Factory, 100, () =>
        {
            for (var i = 0; i < 100; i++)
            {
                using var factory = CreateFactory();
                Sink.Keep(factory.CreateLogger("Harness.Factory"));
            }
        });

        var logger = _logger;
        recorder.Measure(LoggingScopes.Enabled, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                logger.LogInformation(Template, i, "login", i * 0.5);
            }
        });

        recorder.Measure(LoggingScopes.Disabled, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                logger.LogDebug(Template, i, "login", i * 0.5);
            }
        });

        recorder.Measure(LoggingScopes.Define, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                Defined(logger, i, "login", i * 0.5, null);
            }
        });

        recorder.Measure(LoggingScopes.BeginScope, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                using var scope = logger.BeginScope("Request {RequestId}", i);
                Sink.Keep(scope);
            }
        });
    }

    public void Dispose()
    {
        _factory.Dispose();
        _provider.Dispose();
    }

    private ILoggerFactory CreateFactory() =>
        LoggerFactory.Create(builder => builder.AddProvider(_provider).SetMinimumLevel(LogLevel.Information));
}
