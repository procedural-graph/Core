using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;

namespace GameSharp.Sandbox;

public sealed class Application : Guest
{
    public ImmutableTypeLookup Services { get; private set; }

    internal Application(AnonymousPipeClientStream outboundPipe, AnonymousPipeClientStream inboundPipe, 
        JsonRpc jsonRpc, ImmutableTypeLookup services) : base(outboundPipe, inboundPipe, jsonRpc)
    {
        Services = services;
    }

    [RequiresUnreferencedCode("This code uses a formatter/serializer that hasn't been hardened to avoid dynamic code.")]
    [RequiresDynamicCode("This code uses a formatter/serializer that hasn't been hardened to avoid dynamic code.")]
    public static ApplicationBuilder CreateDefaultBuilder(object[] args)
    {
        return new ApplicationBuilder(args);
    }

    protected override async Task OnDisposingAsync()
    {
        Task disposing = base.OnDisposingAsync();
        await disposing.ConfigureAwait(false);

        foreach (IDisposable disposable in Services.GetAll<IDisposable>())
        {
            disposable.Dispose();
        }
    }
}
