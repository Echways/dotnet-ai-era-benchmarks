using Harness.Core;
using Microsoft.Extensions.Logging;

namespace Harness.Scenarios;

public sealed class NoIoLoggerProvider : ILoggerProvider
{
    private readonly NoIoLogger _logger = new();

    public ILogger CreateLogger(string categoryName) => _logger;

    public void Dispose()
    {
    }
}

public sealed class NoIoLogger : ILogger
{
    private static readonly IDisposable Scope = new NoIoScope();

    IDisposable ILogger.BeginScope<TState>(TState state)
    {
        Sink.Keep(state);
        return Scope;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        Sink.Keep(formatter(state, exception).Length);

    private sealed class NoIoScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
