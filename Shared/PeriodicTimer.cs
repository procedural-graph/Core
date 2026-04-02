#if !NET6_0_OR_GREATER
using System.Threading.Tasks;

namespace System.Threading;

internal sealed class PeriodicTimer(TimeSpan period) : IDisposable
{
    private DateTime _lastInvocation = DateTime.MinValue;
    private bool _disposed = false;
    private readonly TimeSpan _period = period;
    public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return false;
        }

        TimeSpan elapsed = DateTime.UtcNow - _lastInvocation;

        if (elapsed < _period)
        {
            try
            {
                Task delay = Task.Delay(_period - elapsed, cancellationToken);
                await delay.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        _lastInvocation = DateTime.UtcNow;
        return true;
    }
    public void Dispose() => _disposed = true;
}
#endif