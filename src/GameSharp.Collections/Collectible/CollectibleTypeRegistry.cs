using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Collectible;

internal sealed class CollectibleTypeRegistry(short id) : TypeRegistry(id), IDisposable
{
    private readonly ReaderWriterLockSlim _syncRoot = new(LockRecursionPolicy.NoRecursion);
    private readonly ManualResetEventSlim _drained = new(initialState: true);
    private int _state;

    public override TypeInfo Get(int id)
    {
        ObjectDisposedException.ThrowIf(!TryIncrementActiveOperations(), this);
        _syncRoot.EnterReadLock();

        try
        {
            return base.Get(id);
        }
        finally
        {
            _syncRoot.ExitReadLock();
            DecrementActiveOperations();
        }
    }

    public override bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
        ObjectDisposedException.ThrowIf(!TryIncrementActiveOperations(), this);
        _syncRoot.EnterUpgradeableReadLock();

        try
        {
            if (Registrations.TryGetValue(type, out typeInfo))
            {
                return true;
            }

            _syncRoot.EnterWriteLock();

            try
            {
                return base.GetOrAdd(type, out typeInfo);
            }
            finally
            {
                _syncRoot.ExitWriteLock();
            }
        }
        finally
        {
            _syncRoot.ExitUpgradeableReadLock();
            DecrementActiveOperations();
        }
    }

    public override bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        ObjectDisposedException.ThrowIf(!TryIncrementActiveOperations(), this);
        _syncRoot.EnterReadLock();

        try
        {
            return base.TryGet(type, out typeInfo);
        }
        finally
        {
            _syncRoot.ExitReadLock();
            DecrementActiveOperations();
        }
    }

    public void Dispose()
    {
        int currState = Interlocked.Exchange(ref _state, -1);

        if (currState == -1)
        {
            return;
        }

        if (currState > 0)
        {
            _drained.Wait();
        }

        _drained.Dispose();
        _syncRoot.Dispose();
    }

    public override PurgeContext GetPurgeContext()
    {
        ObjectDisposedException.ThrowIf(!TryIncrementActiveOperations(), this);
        _syncRoot.EnterReadLock();
        return base.GetPurgeContext();
    }

    protected override void CompletePurge()
    {
        _syncRoot.ExitReadLock();
        DecrementActiveOperations();
    }

    private bool TryIncrementActiveOperations()
    {
        int currState = Volatile.Read(ref _state), prevState;

        do
        {
            if (currState == -1)
            {
                return false;
            }

            int nextState = currState + 1;
            (currState, prevState) = (Interlocked.CompareExchange(ref _state, nextState, currState), currState);
        }
        while (prevState != currState);

        _drained.Reset();

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DecrementActiveOperations()
    {
        if (RuntimeFeature.IsDynamicCodeSupported && Interlocked.Decrement(ref _state) == 0)
        {
            _drained.Set();
        }
    }
}
