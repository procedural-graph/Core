using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.Sandbox;

/// <summary>
/// Represents a guest process that communicates with a host process using JSON-RPC over named pipes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Guest"/> class with the specified reader, writer, and JSON-RPC instance.
/// </remarks>
/// <param name="reader">The pipe stream used for reading data.</param>
/// <param name="writer">The pipe stream used for writing data.</param>
/// <param name="jsonRpc">The JSON-RPC instance for communication.</param>
public class Guest(PipeStream reader, PipeStream writer, JsonRpc jsonRpc) : AsyncLifecycle
{
    /// <inheritdoc/>
    protected override async Task ExecuteAsync()
    {
        using CancellationTokenRegistration reg = StoppingToken.Register(jsonRpc.Dispose);
        Task listen = Task.Factory.StartNew(jsonRpc.StartListening, TaskCreationOptions.LongRunning);
        await listen.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override async Task OnDisposingAsync()
    {
        Task disposing = base.OnDisposingAsync();
        await disposing.ConfigureAwait(false);

        jsonRpc.Dispose();
        reader.Dispose();
        writer.Dispose();
    }
}