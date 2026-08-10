using GameSharp.Sandbox.Dotnet;
using Microsoft.Extensions.Logging;
using SandboxEscape;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace GameSharp.Sandbox.Tests;

[StructLayout(LayoutKind.Sequential)]
public sealed partial class TestFixture : AsyncLifecycle, IAsyncLifetime, IDisposable
{
    private readonly Host _host;
    private readonly LoggerProvider _loggerProvider;
    private readonly JsonRpcFactory _factory;
    private readonly string _assemblyDirectory;
    private Task _execute;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    protected override ReadOnlySpan<AsyncLifetime> Descendants => FromContiguousFieldsUnsafe(in _host, 2);
#else
    private readonly AsyncLifetime[] _descendants;
    protected override ReadOnlySpan<AsyncLifetime> Descendants => _descendants.AsSpan();
#endif

    public ISandboxTest SandboxTest { get; private set; }

    public ILogger Logger { get; }

    public TestFixture() : base()
    {
        _execute = null!;
        SandboxTest = default!;

        _assemblyDirectory = Path.Combine(AppContext.BaseDirectory, "Guest");
        if (Platform.IsWindows())
        {
            string tempDir = Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Local\_Guest");
            Directory.CreateDirectory(tempDir);
            foreach (string subdir in Directory.GetDirectories(_assemblyDirectory, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(subdir.Replace(_assemblyDirectory, tempDir));
            }
            foreach (string filePath in Directory.GetFiles(_assemblyDirectory, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(filePath, filePath.Replace(_assemblyDirectory, tempDir), overwrite: true);
            }
            _assemblyDirectory = tempDir;
        }

        _loggerProvider = new LoggerProvider();
        ILogger local = _loggerProvider.CreateLogger("Local"), remote = _loggerProvider.CreateLogger("Remote");
        Logger = local;
        _factory = new JsonRpcFactory(local, remote);

        HostBuilder builder = Host.CreateDefaultBuilder(_factory);
        builder.AddDotnet();
        _host = builder.Build();

#if !NETCOREAPP2_1_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
        _descendants = [_host, _loggerProvider];
#endif
    }

    protected override async Task OnStartingAsync()
    {
        Task starting = base.OnStartingAsync();
        await starting.ConfigureAwait(false);

        string assemblyPath = Path.Combine(_assemblyDirectory, "SandboxEscape.Client.dll");
        LogGuestStarting(Logger);
        _execute = _host.ExecuteAssemblyAsync(assemblyPath, StoppingToken);

        Task<ISandboxTest> getTest = _factory.GetSandboxTestAsync();
        SandboxTest = await getTest.ConfigureAwait(false);
    }

    protected override Task OnStartedAsync() => _execute;

    protected override void OnDisposing()
    {
        base.OnDisposing();
        _host.Dispose();
        if (Platform.IsWindows())
        {
            try
            {
                Directory.Delete(_assemblyDirectory, recursive: true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to delete temporary directory '{TempDir}'.", _assemblyDirectory);
            }
        }
        _loggerProvider.Dispose();
    }

    protected override void HandleException(Exception ex)
    {
        if (ex is AggregateException aggregateException)
        {
            foreach (Exception innerException in aggregateException.InnerExceptions)
            {
                HandleException(innerException);
            }
        }
        else
        {
            Logger.LogError(ex, "An exception occurred during test fixture execution.");
        }
    }

    [LoggerMessage(LogLevel.Information, EventId = 300, EventName = "GuestStarting", Message = "Guest starting.")]
    private static partial void LogGuestStarting(ILogger logger);

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    async Task IAsyncLifetime.InitializeAsync() => await StartAsync();
}
