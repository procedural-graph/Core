using StreamJsonRpc;
using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace GameSharp.Sandbox.Generic;

internal abstract class Guest<TArgs, TProcessInfo>(PipeStream reader, PipeStream writer, JsonRpc jsonRpc, Host host) : Guest(reader, writer, jsonRpc)
    where TArgs : struct, ICommandLineArguments
    where TProcessInfo : IDisposable
{
    protected override async Task OnStartingAsync()
    {
        Task starting = base.OnStartingAsync();
        await starting.ConfigureAwait(false);

        using TProcessInfo processInfo = WaitForProcessExit(out bool success);

        if (success)
        {
            return;
        }

        StringBuilder sb = host.StringBuilderPool.Get();
        try
        {
            throw CreateProcessException(processInfo, sb);
        }
        finally
        {
            host.StringBuilderPool.Return(sb);
        }
    }

    protected string BuildCommandLineArguments(scoped ref readonly TArgs args)
    {
        StringBuilder sb = host.StringBuilderPool.Get();
        try
        {
            return args.ToString(sb);
        }
        finally
        {
            host.StringBuilderPool.Return(sb);
        }
    }

    protected abstract TProcessInfo WaitForProcessExit(out bool success);

    protected abstract ProcessException CreateProcessException(TProcessInfo processInfo, StringBuilder sb);
}
