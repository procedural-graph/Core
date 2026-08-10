using Microsoft.Extensions.Logging;
using System;
using System.Threading.Channels;

namespace SandboxEscape;

internal sealed class Logger(ChannelWriter<LogEntry> output, string categoryName) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }
        string message = formatter(state, exception);
        output.TryWrite(new LogEntry(logLevel, categoryName, message, exception));
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}
