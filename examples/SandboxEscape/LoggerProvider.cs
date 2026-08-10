using GameSharp;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SandboxEscape;

public sealed class LoggerProvider : AsyncLifecycle, ILoggerProvider
{
    private sealed class LogWriter : Disposable
    {
        private readonly string _logFilePath;
        private readonly StreamWriter _writer;
        private bool _preserve;

        public LogWriter()
        {
            _logFilePath = Path.Combine(AppContext.BaseDirectory, $"test-log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            FileStream fileStream = new(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(fileStream, Encoding.UTF8, bufferSize: 4096, leaveOpen: false);
        }

        public void Write(scoped ref readonly LogEntry entry)
        {
            _preserve |= entry.Level >= LogLevel.Warning;
            _writer.Write('[');
            _writer.Write(entry.Level);
            _writer.Write("] ");
            _writer.Write(entry.Category);
            _writer.Write(": ");
            _writer.WriteLine(entry.Message);
            if (entry.Exception is not null)
            {
                _writer.WriteLine(entry.Exception);
            }
        }

        protected override void OnDisposing()
        {
            _writer.Dispose();
            if (!_preserve)
            {
                File.Delete(_logFilePath);
            }
        }
    }

    private readonly static UnboundedChannelOptions _channelOptions = new()
    {
        SingleReader = true,
        SingleWriter = false
    };

    private readonly Channel<LogEntry> _logChannel = Channel.CreateUnbounded<LogEntry>(_channelOptions);

    public ILogger CreateLogger(string categoryName)
    {
        return new Logger(_logChannel.Writer, categoryName);
    }

    public static async Task<LoggerProvider> CreateAsync(CancellationToken cancellationToken = default)
    {
        LoggerProvider provider = new();
        Task start = provider.StartAsync(cancellationToken);
        await start.ConfigureAwait(false);
        return provider;
    }

    public static LoggerProvider Create()
    {
        LoggerProvider provider = new();
        provider.Start();
        return provider;
    }

    protected override async Task ExecuteAsync()
    {
        ChannelReader<LogEntry> reader = _logChannel.Reader;
        if (!await reader.WaitToReadAsync(StoppingToken).ConfigureAwait(false))
        {
            return;
        }
        using LogWriter writer = new();
        await foreach (LogEntry entry in reader.ReadAllAsync(StoppingToken).ConfigureAwait(false))
        {
            writer.Write(in entry);
        }
    }

    protected override async Task OnStoppingAsync()
    {
        try
        {
            Task stopping = base.OnStoppingAsync();
            await stopping.ConfigureAwait(false);
        }
        finally
        {
            _logChannel.Writer.TryComplete();
        }
    }
}
