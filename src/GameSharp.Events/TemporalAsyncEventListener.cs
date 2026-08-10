using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Events;

internal sealed class TemporalAsyncEventListener<TSender>(AsyncEventHandler<TSender, TimeSpan> eventHandler, ILogger logger) :
    AsyncEventListener<TSender, TimeSpan>(eventHandler),
    IAsyncEventListener<TSender>
{
    private readonly ILogger _logger = logger;
    private long _lastCallTime;
    private Task _process = Task.CompletedTask;
    private bool _isCritical;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Invoke(TSender sender, CancellationToken cancellationToken)
    {
        return Invoke(sender, out _, cancellationToken);
    }

    public async ValueTask InvokeAsync(TSender sender, CancellationToken cancellationToken)
    {
        while (!Invoke(sender, out Task currentProcess, cancellationToken))
        {
            ObjectDisposedException.ThrowIf(Disposal.IsStarted, this);
            Task wait = currentProcess.WaitAsync(cancellationToken);
            await wait.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            cancellationToken.ThrowIfCancellationRequested();
            if (wait.IsFaulted)
            {
                Throw(wait.Exception);
            }
        }
    }

    protected override Task OnStartingAsync()
    {
        _lastCallTime = Stopwatch.GetTimestamp();
        return base.OnStartingAsync();
    }

    private bool Invoke(TSender sender, out Task currentProcess, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Disposal.IsStarted, this);

        currentProcess = Volatile.Read(ref _process);
        if (!currentProcess.IsCompleted)
        {
            return false;
        }

        if (!Interlocked.CompareExchange(ref _isCritical, true, false))
        {
            return true;
        }

        ValueTask newProcess;
        try
        {
            (Task previousProcess, currentProcess) = (currentProcess, Volatile.Read(ref _process));
            if (!ReferenceEquals(previousProcess, currentProcess) && !currentProcess.IsCompleted)
            {
                return false;
            }

            long now = Stopwatch.GetTimestamp();
            TimeSpan deltaTime = Stopwatch.GetElapsedTime(_lastCallTime, now);
            _lastCallTime = now;
            newProcess = EventHandler.Invoke(sender, deltaTime, cancellationToken);

            if (!newProcess.IsCompleted)
            {
                Task continuation = newProcess.AsTask().ContinueWith(
                    static (task, state) => EventHandlerThrewAnException((ILogger)state!, task.Exception!), 
                    _logger, 
                    StoppingToken,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, 
                    TaskScheduler.Default);
                Volatile.Write(ref _process, continuation);
                return true;
            }

            Volatile.Write(ref _process, Task.CompletedTask);
        }
        finally
        {
            Volatile.Write(ref _isCritical, false);
        }

        try
        {
            newProcess.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException) { } // Ignore
        catch (Exception ex)
        {
            EventHandlerThrewAnException(_logger, ex);
        }

        return true;
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw(Exception exception)
    {
        throw exception;
    }
}
