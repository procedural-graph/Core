using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

internal sealed class TypeRegistry(short id) : IDisposable
{
    public ref struct PurgeContext
    {
        private bool _disposed;

        public TypeRegistry Registry { get; init; }

        public int Count { get; init; }

        public readonly void Purge(ref Task task, short assemblyID)
        {
            OrderedDictionary<Type, TypeInfo>.ValueCollection.Enumerator enumerator = Registry._registrations.Values.GetEnumerator();
            for (; enumerator.MoveNext(); task = ref Unsafe.Add(ref task, 1))
            {
                DerivedTypeCollection derived = enumerator.Current.Derived;
                task = Task.Run(() => derived.RemoveAll(assemblyID));
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Registry._syncRoot.ExitReadLock();
                Registry.DecrementActiveOperations();
            }

            _disposed = true;
        }
    }

    public short AssemblyID { get; } = id;

    private readonly ReaderWriterLockSlim _syncRoot = new(LockRecursionPolicy.NoRecursion);
    private readonly ManualResetEventSlim _drained = RuntimeFeature.IsDynamicCodeSupported ? new(initialState: true) : null!;
    private int _state;
    private bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => RuntimeFeature.IsDynamicCodeSupported && !TryIncrementActiveOperations();
    }
    private readonly OrderedDictionary<Type, TypeInfo> _registrations = [];

    public TypeInfo Get(int id)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _syncRoot.EnterReadLock();

        try
        {
            return _registrations.GetAt(id).Value;
        }
        finally
        {
            _syncRoot.ExitReadLock();
            DecrementActiveOperations();
        }
    }

    public bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _syncRoot.EnterReadLock();

        try
        {
            return _registrations.TryGetValue(type, out typeInfo);
        }
        finally
        {
            _syncRoot.ExitReadLock();
            DecrementActiveOperations();
        }
    }

    public bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _syncRoot.EnterUpgradeableReadLock();

        try
        {
            if (_registrations.TryGetValue(type, out typeInfo))
            {
                return true;
            }

            _syncRoot.EnterWriteLock();

            try
            {
                if (_registrations.TryGetValue(type, out typeInfo))
                {
                    return true;
                }

                TypeIdentifier id = new()
                {
                    AssemblyID = AssemblyID,
                    TypeID = checked((ushort)_registrations.Count)
                };

                typeInfo = new TypeInfo(type, id);
                _registrations.Add(type, typeInfo);
            }
            catch (OverflowException ex)
            {
                throw new InvalidOperationException("The maximum number of TypeInfo instances has been reached.", ex);
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

        return false;
    }

    public PurgeContext GetPurgeContext()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        _syncRoot.EnterReadLock();
        return new PurgeContext()
        {
            Count = _registrations.Count,
            Registry = this
        };
    }

    public void Dispose()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
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
        else
        {
            throw new NotSupportedException();
        }
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
