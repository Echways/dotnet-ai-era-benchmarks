using EmberTrace.Abstractions.Attributes;
using Harness.Core;
using Harness.Scenarios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

[assembly: TraceId(LoggingScopes.ColdFactory, "logging.cold.factory-create", "logging")]
[assembly: TraceId(LoggingScopes.ColdFirstLog, "logging.cold.first-log", "logging")]
[assembly: TraceId(LoggingScopes.Factory, "logging.factory-create.x100", "logging")]
[assembly: TraceId(LoggingScopes.Enabled, "logging.log-information-3args-enabled.x10k", "logging")]
[assembly: TraceId(LoggingScopes.Disabled, "logging.log-debug-3args-disabled.x10k", "logging")]
[assembly: TraceId(LoggingScopes.Define, "logging.logger-message-define.x10k", "logging")]
[assembly: TraceId(LoggingScopes.BeginScope, "logging.begin-scope.x10k", "logging")]
[assembly: TraceId(LoggingScopes.NullLogger, "logging.null-logger.x10k", "logging")]
[assembly: TraceId(LoggingScopes.LogError, "logging.log-error-3args-enabled.x10k", "logging")]
[assembly: TraceId(LoggingScopes.DefinedScope, "logging.logger-message-define-scope.x10k", "logging")]
[assembly: TraceId(LoggingScopes.DefineCreate, "logging.logger-message-define-create.x1k", "logging")]
[assembly: TraceId(LoggingScopes.AddLogging, "logging.add-logging-set-minimum-level.x1k", "logging")]

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
    public const int NullLogger = 1315;
    public const int LogError = 1316;
    public const int DefinedScope = 1317;
    public const int DefineCreate = 1318;
    public const int AddLogging = 1319;
}

public sealed class LoggingScenario : IScenario
{
    private const int Batch = 10_000;
    private const string Template = "User {UserId} performed {Action} in {Elapsed} ms";

    private static readonly Action<ILogger, int, string, double, Exception?> Defined =
        LoggerMessage.Define<int, string, double>(LogLevel.Information, new EventId(7, "Performed"), Template);

    private static readonly Func<int, Exception?, string> NullFormatter = static (state, _) => "message";

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

        recorder.Measure(LoggingScopes.NullLogger, Batch, () =>
        {
            var nullLogger = NullLogger.Instance;
            var nullFactory = NullLoggerFactory.Instance;
            for (var i = 0; i < Batch; i++)
            {
                nullLogger.Log(LogLevel.Information, default, i, null, NullFormatter);
                Sink.Keep(nullLogger.IsEnabled(LogLevel.Information) ? 1 : 0);
                Sink.Keep(nullFactory.CreateLogger("Harness.Null"));
            }
        });

        recorder.Measure(LoggingScopes.LogError, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                logger.LogError(Template, i, "login", i * 0.5);
            }
        });

        var scopeDefinition = LoggerMessage.DefineScope<int>("Request {RequestId}");
        recorder.Measure(LoggingScopes.DefinedScope, Batch, () =>
        {
            for (var i = 0; i < Batch; i++)
            {
                using var scope = scopeDefinition(logger, i);
                Sink.Keep(scope);
            }
        });

        recorder.Measure(LoggingScopes.DefineCreate, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                Sink.Keep(LoggerMessage.Define<int, string, double>(LogLevel.Information, new EventId(7, "Performed"), Template));
                Sink.Keep(LoggerMessage.DefineScope<int>("Request {RequestId}"));
            }
        });

        recorder.Measure(LoggingScopes.AddLogging, 1000, () =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Information));
                Sink.Keep(services);
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
