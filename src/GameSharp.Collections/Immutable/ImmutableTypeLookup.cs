using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Immutable;

/// <summary>
/// Represents an immutable collection of objects, providing efficient lookup and retrieval by object type.
/// </summary>
/// <inheritdoc/>
public sealed class ImmutableTypeLookup : ReadOnlyTypeLookup
{
    /// <summary>
    /// A builder for creating an <see cref="ImmutableTypeLookup"/> instance.
    /// </summary>
    public sealed class Builder : TypeLookup
    {
        private new readonly ref struct ArrayBuilder<T>(ref bool lockHeld, Builder collectionBuilder, TypeLookup.ArrayBuilder<T> arrayBuilder) : IArrayBuilder<T>
        {
            private readonly TypeLookup.ArrayBuilder<T> _arrayBuilder = arrayBuilder;
            private readonly ref bool _lockHeld = ref lockHeld;

            public T[] Array => _arrayBuilder.Array;

            public int LogicalCount
            {
                get => _arrayBuilder.LogicalCount;
                set => _arrayBuilder.LogicalCount = value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Mutate()
            {
                if (!_lockHeld)
                {
                    collectionBuilder._syncRoot.Enter();
                    collectionBuilder.ThrowIfAlreadyBuilt();
                    _lockHeld = true;
                }

                _arrayBuilder.Mutate();
            }
        }

        private volatile bool _isBuilt;
        private readonly Lock _syncRoot = new();

        internal Builder() : base()
        {
        }

        internal Builder(ReadOnlySpan<IntegerLookup> lookups, ReadOnlySpan<object> items)
            : base([.. lookups], lookups.Length, [.. items], items.Length)
        {
        }

        /// <summary>
        /// Converts the builder to an immutable <see cref="ImmutableTypeLookup"/> instance.
        /// </summary>
        /// <remarks>
        /// This method can only be called once. After calling this method, the builder becomes immutable and cannot be modified further.
        /// </remarks>
        /// <returns>An immutable <see cref="ImmutableTypeLookup"/> instance containing the same data as the builder.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the builder has already been built.</exception>
        public ImmutableTypeLookup ToImmutable()
        {
            _syncRoot.Enter();
            try
            {
                ThrowIfAlreadyBuilt();
                _isBuilt = true;
                return new ImmutableTypeLookup(lookups, lookupCount, items, itemCount);
            }
            finally
            {
                _syncRoot.Exit();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool Add(object item, ITypeInfo typeInfo)
        {
            bool lockHeld = false;
            try
            {
                GetBuilders(ref lockHeld, out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder);
                Add(ref lookupBuilder, ref itemBuilder, item, typeInfo);
                return lockHeld;
            }
            finally
            {
                if (lockHeld)
                {
                    _syncRoot.Exit();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override bool Remove(object item, ITypeInfo typeInfo)
        {
            bool lockHeld = false;
            try
            {
                GetBuilders(ref lockHeld, out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder);
                Remove(ref lookupBuilder, ref itemBuilder, item, typeInfo);
                return lockHeld;
            }
            finally
            {
                if (lockHeld)
                {
                    _syncRoot.Exit();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GetBuilders(ref bool lockHeld, out ArrayBuilder<IntegerLookup> lookupBuilder, out ArrayBuilder<object> itemBuilder)
        {
            GetBuilders(out TypeLookup.ArrayBuilder<IntegerLookup> baseLookupBuilder, out TypeLookup.ArrayBuilder<object> baseItemBuilder);
            lookupBuilder = new ArrayBuilder<IntegerLookup>(ref lockHeld, this, baseLookupBuilder);
            itemBuilder = new ArrayBuilder<object>(ref lockHeld, this, baseItemBuilder);
        }

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfAlreadyBuilt()
        {
            if (_isBuilt)
            {
                BuilderIsAlreadyBuilt();
            }
        }

        [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
        private static void BuilderIsAlreadyBuilt()
        {
            throw new InvalidOperationException("The builder has already been built and cannot be modified.");
        }
    }

    private struct ArrayBuilder<T>(T[] array, int logicalCount) : IArrayBuilder<T>
    {
        private readonly T[] _srcArray = array;

        public T[] Array { get; private set; } = array;

        public int LogicalCount { get; set; } = logicalCount;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Mutate()
        {
            if (ReferenceEquals(Array, _srcArray))
            {
                if (LogicalCount > Array.Length)
                {
                    Grow();
                }
                else
                {
                    Array = Array[..LogicalCount];
                }
            }
            else if (LogicalCount > Array.Length)
            {
                Grow();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Grow()
        {
            T[] newArray = GC.AllocateUninitializedArray<T>(LogicalCount);
            Array.CopyTo(newArray, 0);
            Array = newArray;
        }
    }

    private readonly IntegerLookup[] _lookups;
    private readonly int _lookupCount;
    internal override ReadOnlyMemory<IntegerLookup> Lookups => _lookups.AsMemory(0, _lookupCount);

    private readonly object[] _items;
    private readonly int _itemCount;
    internal override ReadOnlyMemory<object> Items => _items.AsMemory(0, _itemCount);

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableTypeLookup"/> class that is empty.
    /// </summary>
    public ImmutableTypeLookup()
    {
        _lookups = [];
        _items = [];
    }

    internal ImmutableTypeLookup(IntegerLookup[] lookups, int lookupCount, object[] items, int itemCount)
    {
        _lookups = lookups;
        _lookupCount = lookupCount;
        _items = items;
        _itemCount = itemCount;
    }

    /// <typeparam name="T"> The type of the item to add. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Add(object, Type)"/>
    public ImmutableTypeLookup Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);

        ITypeInfo typeInfo = GetTypeInfo<T>();
        return Add(item, typeInfo);
    }

    /// <returns>
    /// A new <see cref="ImmutableTypeLookup"/> that contains the specified item, or the current collection if the item is
    /// already present.
    /// </returns>
    /// <inheritdoc cref="TypeLookup.Add(object, Type)"/>
    [RequiresDynamicCode(RequiresDynamicCodeMessage)]
    public ImmutableTypeLookup Add(object item, Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);

        ITypeInfo typeInfo = GetTypeInfo(type);
        return Add(item, typeInfo);
    }

    /// <typeparam name="T">The type of the item to remove. Must be a reference type.</typeparam>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="item"/> is <see langword="null"/>.</exception>
    /// <inheritdoc cref="Remove(object, Type)"/>
    public ImmutableTypeLookup Remove<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>(T item) where T : class
    {
        ArgumentNullException.ThrowIfNull(item);

        ITypeInfo typeInfo = GetTypeInfo<T>();
        return Remove(item, typeInfo);
    }

    /// <returns>
    /// A new <see cref="ImmutableTypeLookup"/> with the specified item removed; or the current 
    /// collection if the item is not found.
    /// </returns>
    /// <inheritdoc cref="TypeLookup.Remove(object, Type)"/>
    public ImmutableTypeLookup Remove(object item, Type type)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(type);

        if (TryGetTypeInfo(type, out ITypeInfo? typeInfo))
        {
            return Remove(item, typeInfo);
        }

        return this;
    }

    /// <summary>
    /// Creates a new <see cref="Builder"/> instance.
    /// </summary>
    /// <returns>A new <see cref="Builder"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Builder CreateBuilder()
    {
        return [];
    }

    /// <summary>
    /// Atomically updates the <see cref="ImmutableTypeLookup"/> instance at the specified location using the provided transformation function.
    /// </summary>
    /// <inheritdoc cref="InterlockedUpdate{TState1}(ref ImmutableTypeLookup, Func{ImmutableTypeLookup, TState1, ImmutableTypeLookup}, TState1)"/>
    public static bool InterlockedUpdate(ref ImmutableTypeLookup location1, Func<ImmutableTypeLookup, ImmutableTypeLookup> transformation)
    {
        ImmutableTypeLookup currLookup = Volatile.Read(ref location1), prevLookup;
        do
        {
            ImmutableTypeLookup newLookup = transformation(currLookup);
            if (ReferenceEquals(currLookup, newLookup))
            {
                return false;
            }
            (prevLookup, currLookup) = (currLookup, Interlocked.CompareExchange(ref location1, newLookup, currLookup));
        }
        while (!ReferenceEquals(prevLookup, currLookup));
        return true;
    }

    /// <inheritdoc cref="InterlockedUpdate{TState1, TState2}(ref ImmutableTypeLookup, Func{ImmutableTypeLookup, TState1, TState2, ImmutableTypeLookup}, TState1, TState2)"/>
    public static bool InterlockedUpdate<TState1>(ref ImmutableTypeLookup location1, 
        Func<ImmutableTypeLookup, TState1, ImmutableTypeLookup> transformation, 
        TState1 state1) 
        where TState1 : allows ref struct
    {
        ImmutableTypeLookup currLookup = Volatile.Read(ref location1), prevLookup;
        do
        {
            ImmutableTypeLookup newLookup = transformation(currLookup, state1);
            if (ReferenceEquals(currLookup, newLookup))
            {
                return false;
            }
            (prevLookup, currLookup) = (currLookup, Interlocked.CompareExchange(ref location1, newLookup, currLookup));
        }
        while (!ReferenceEquals(prevLookup, currLookup));
        return true;
    }

    /// <summary>
    /// Atomically updates the <see cref="ImmutableTypeLookup"/> instance at the specified location using the provided transformation function and state.
    /// </summary>
    /// <typeparam name="TState1">The type of the state parameter.</typeparam>
    /// <typeparam name="TState2">The type of the state parameter.</typeparam>
    /// <param name="location1">The location of the <see cref="ImmutableTypeLookup"/> instance to update.</param>
    /// <param name="transformation">The transformation function to apply.</param>
    /// <param name="state1">The state to pass to the transformation function.</param>
    /// <param name="state2">The state to pass to the transformation function.</param>
    /// <returns><see langword="true"/> if the update was successful; otherwise, <see langword="false"/>.</returns>
    public static bool InterlockedUpdate<TState1, TState2>(ref ImmutableTypeLookup location1, 
        Func<ImmutableTypeLookup, TState1, TState2, ImmutableTypeLookup> transformation, 
        TState1 state1,
        TState2 state2) 
        where TState1 : allows ref struct
        where TState2 : allows ref struct
    {
        ImmutableTypeLookup currLookup = Volatile.Read(ref location1), prevLookup;
        do
        {
            ImmutableTypeLookup newLookup = transformation(currLookup, state1, state2);
            if (ReferenceEquals(currLookup, newLookup))
            {
                return false;
            }
            (prevLookup, currLookup) = (currLookup, Interlocked.CompareExchange(ref location1, newLookup, currLookup));
        }
        while (!ReferenceEquals(prevLookup, currLookup));
        return true;
    }

    private ImmutableTypeLookup Add(object item, ITypeInfo typeInfo)
    {
        ArrayBuilder<IntegerLookup> lookupBuilder = new(_lookups, _lookupCount);
        ArrayBuilder<object> itemBuilder = new(_items, _itemCount);

        TypeLookup.Add(ref lookupBuilder, ref itemBuilder, item, typeInfo);

        if (ReferenceEquals(lookupBuilder.Array, _lookups) && ReferenceEquals(itemBuilder.Array, _items))
        {
            return this;
        }

        return new ImmutableTypeLookup(lookupBuilder.Array, lookupBuilder.LogicalCount, itemBuilder.Array, itemBuilder.LogicalCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ImmutableTypeLookup Remove(object item, ITypeInfo typeInfo)
    {
        ArrayBuilder<IntegerLookup> lookupBuilder = new(_lookups, _lookupCount);
        ArrayBuilder<object> itemBuilder = new(_items, _itemCount);

        TypeLookup.Remove(ref lookupBuilder, ref itemBuilder, item, typeInfo);

        if (ReferenceEquals(lookupBuilder.Array, _lookups) && ReferenceEquals(itemBuilder.Array, _items))
        {
            return this;
        }

        return new ImmutableTypeLookup(lookupBuilder.Array, lookupBuilder.LogicalCount, itemBuilder.Array, itemBuilder.LogicalCount);
    }
}
