using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections.Static;

internal sealed class TypeRegistry : Collections.TypeRegistry
{
    private readonly ReaderWriterLockSlim _syncRoot = new(LockRecursionPolicy.NoRecursion);

    public override TypeInfo Get(int id)
    {
        _syncRoot.EnterReadLock();

        try
        {
            return base.Get(id);
        }
        finally
        {
            _syncRoot.ExitReadLock();
        }
    }

    public override bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        _syncRoot.EnterReadLock();

        try
        {
            return base.TryGet(type, out typeInfo);
        }
        finally
        {
            _syncRoot.ExitReadLock();
        }
    }

    public override bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
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
        }
    }
}
