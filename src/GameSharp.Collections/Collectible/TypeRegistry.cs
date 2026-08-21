using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Collectible;

internal abstract class TypeRegistry(short id) : Collections.TypeRegistry
{
    public short AssemblyID { get; } = id;

    protected override int NextID
    {
        get
        {
            TypeIdentifier identifier = new(AssemblyID, base.NextID);
            return identifier.CompositeKey;
        }
    }

    public ref struct PurgeContext
    {
        private bool _disposed;

        public TypeRegistry Registry { get; init; }

        public int Count { get; init; }

        public readonly void Purge(ref Task task, short assemblyID)
        {
            OrderedDictionary<Type, TypeInfo>.ValueCollection.Enumerator enumerator = Registry.Registrations.Values.GetEnumerator();
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
                Registry.CompletePurge();
            }

            _disposed = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual PurgeContext GetPurgeContext() => new()
    {
        Count = Registrations.Count,
        Registry = this
    };

    protected abstract void CompletePurge();
}
