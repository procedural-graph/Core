using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.Sandbox;

public abstract class Host : AsyncLifecycle
{
    internal DefaultObjectPool<StringBuilder> StringBuilderPool { get; } = new(new StringBuilderPooledObjectPolicy());
    private readonly ConcurrentDictionary<AsyncLifecycle, CancellationTokenRegistration> _executingInstances = new();

    public abstract Task<CancellationTokenRegistration> ExecuteAssemblyAsync(string assemblyPath, CancellationToken cancellationToken = default);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool AddInstance(Guest instance)
    {
        CancellationTokenRegistration reg = StoppingToken.Register(instance.Stop);

        if (_executingInstances.TryAdd(instance, reg))
        {
            return true;
        }

        reg.Dispose();
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool RemoveInstance(Guest instance)
    {
        if (_executingInstances.TryRemove(instance, out CancellationTokenRegistration reg))
        {
            reg.Dispose();
            return true;
        }

        return false;
    }

    protected override async Task OnStoppingAsync()
    {
        Task selfStop = base.OnStoppingAsync();
        await selfStop.ConfigureAwait(false);

        Task insStop = WhenAll(_executingInstances.Keys);
        await insStop.ConfigureAwait(false);
    }

    protected override async Task OnDisposingAsync()
    {
        Task selfDispose = base.OnDisposingAsync();
        await selfDispose.ConfigureAwait(false);

        Task insDispose = DisposeAll(_executingInstances.Keys);
        await insDispose.ConfigureAwait(false);
    }

    public static HostBuilder CreateDefaultBuilder(IJsonRpcFactory jsonRpcFactory)
    {
        HostBuilder builder = new(jsonRpcFactory);
        builder.AddProcessFactory<NativeProcessFactory>();
        return builder;
    }
}
