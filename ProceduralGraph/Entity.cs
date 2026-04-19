using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if !NET8_0_OR_GREATER
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource<object?>;
#endif

namespace ProceduralGraph;

/// <summary>
/// Represents a base class for entities within a graph structure, providing identity, parent-child relationships,
/// and lifecycle management.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="Entity"/> class.</remarks>
/// <param name="logger">The logger instance used for recording diagnostic and operational messages.</param>
public class Entity(ILogger logger) : AsyncLifecycle, IEquatable<Entity>, ICollection<object>
{
    private readonly struct AsyncDisposalTaskEnumerator(Repository.Query<IAsyncDisposable> query) : IEnumerator<ValueTask>
    {
        private readonly Repository.Query<IAsyncDisposable>.Enumerator _enumerator = query.GetEnumerator();
        public readonly ValueTask Current => _enumerator.Current.DisposeAsync();
        readonly object IEnumerator.Current => Current;
        public bool MoveNext() => _enumerator.MoveNext();
        readonly void IDisposable.Dispose() { }
        readonly void IEnumerator.Reset() => ThrowHelpers.ThrowNotSupportedException(this);
    }

    private static readonly ConcurrentDictionary<Guid, Entity> _entities = [];

    private readonly Guid _id;
    /// <summary>
    /// Gets the unique identifier for this graph entity.
    /// </summary>
    [Serialize]
    public Guid ID
    {
        get => _id;
        init
        {
            _id = value;
            _entities[ID] = this;
        }
    }

    /// <summary>
    /// Gets the logger instance used for recording diagnostic and operational messages.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    private Entity? _parent;
    /// <summary>
    /// Gets the parent entity of this graph entity, if any.
    /// </summary>
    [Serialize]
    public Entity? Parent
    {
        get => Volatile.Read(ref _parent);
        set
        {
            Entity? oldValue = Interlocked.Exchange(ref _parent, value);

            if (Equals(oldValue, value) || Disposed)
            {
                return;
            }

            OnParentChanged(oldValue, value);
        }
    }

    [Serialize]
    internal Repository objects = [];

    /// <inheritdoc/>
    public int Count => objects.Count;

    bool ICollection<object>.IsReadOnly => false;

    /// <inheritdoc cref="Repository.TryGetOne{T}(out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne<T>([NotNullWhen(true)] out T? result) where T : class
    {
        result = default;
        return !Disposed && objects.TryGetOne(out result);
    }

    /// <inheritdoc cref="Repository.GetOne{T}()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOne<T>() where T : class
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return objects.GetOne<T>();
    }

    /// <inheritdoc cref="Repository.TryGetOne(Type, out object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out object? result)
    {
        result = default;
        return !Disposed && objects.TryGetOne(type, out result);
    }

    /// <inheritdoc cref="Repository.GetOne(Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object GetOne(Type type) => objects.GetOne(type);

    /// <returns>
    /// <see langword="true"/> if the item was added to the collection; <see langword="false"/> if the item 
    /// was already present.
    /// </returns>
    /// <inheritdoc cref="Repository.Add{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add<T>(T item) where T : class
    {
        ThrowHelpers.ThrowIfDisposed(Disposed, this);
        return Add(item, typeof(T));
    }

    /// <inheritdoc cref="Add(object, Type, out Repository, out Repository)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Add(object item, Type type)
    {
        return Add(item, type, out _, out _);
    }

    /// <returns>
    /// <see langword="true"/> if the item was found and removed from the collection; otherwise, 
    /// <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="Repository.Remove{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove<T>(T item) where T : class => Remove(item, typeof(T));

    /// <inheritdoc cref="Remove(object, Type, out Repository, out Repository)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Remove(object item, Type type)
    {
        return Remove(item, type, out _, out _);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(object item) => objects.Contains(item, item.GetType());

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(object[] array, int arrayIndex) => objects.Values.CopyTo(array, arrayIndex);

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<object>.Enumerator GetEnumerator() => objects.Values.GetEnumerator();

    /// <inheritdoc/>
    public bool Equals(Entity? other)
    {
        return ReferenceEquals(this, other) || (other is { } && ID == other.ID);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Entity other && Equals(other);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{GetType().Name} ({ID})";
    }

    /// <param name="previous">The repository before the add operation.</param>
    /// <param name="current">The repository after the add operation.</param>
    /// <returns>
    /// <see langword="true"/> if the item was added to the collection; <see langword="false"/> if the item 
    /// was already present.
    /// </returns>
    /// <inheritdoc cref="Repository.Add(object, Type)"/>
    /// <param name="item"/>
    /// <param name="type"/>
    protected bool Add(object item, Type type, out Repository previous, out Repository current)
    {
        current = Volatile.Read(ref objects);
        do
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            (current, previous) = (current.Add(item, type), current);
            if (ReferenceEquals(previous, current))
            {
                return false;
            }
            current = Interlocked.CompareExchange(ref objects, current, previous);
        }
        while (!ReferenceEquals(previous, current));
        return true;
    }

    /// <param name="previous">The repository before the removal operation.</param>
    /// <param name="current">The repository after the removal operation.</param>
    /// <returns>
    /// <see langword="true"/> if the item was found and removed from the collection; otherwise, 
    /// <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="Repository.Remove(object, Type)"/>
    /// <param name="item"/>
    /// <param name="type"/>
    protected bool Remove(object item, Type type, out Repository previous, out Repository current)
    {
        current = Volatile.Read(ref objects);
        do
        {
            ThrowHelpers.ThrowIfDisposed(Disposed, this);
            (current, previous) = (current.Remove(item, type), current);
            if (ReferenceEquals(previous, current))
            {
                return false;
            }
            current = Interlocked.CompareExchange(ref objects, current, previous);
        }
        while (!ReferenceEquals(previous, current));
        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (AsyncLifecycle lifecycle in this.objects.GetAll<AsyncLifecycle>())
        {
            lifecycle.TryStart();
        }

        TaskCompletionSource tcs = new();
        cancellationToken.Register(CompleteLifecycle, tcs);
        await tcs.Task;

        try
        {
            await OnStoppingAsync();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, this);
        }

        if (!Clear(out Repository? objects))
        {
            return;
        }
       
        try
        {
            AsyncDisposalTaskEnumerator enumerator = new(objects.GetAll<IAsyncDisposable>());
            await Extensions.CompleteAllAsync(enumerator);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, this);
        }
    }

    /// <summary>Handles logic to be executed asynchronously when this entity is stopping.</summary>
    /// <remarks>This method is called before any descendant items are disposed.</remarks>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous operation.</returns>
    protected virtual async ValueTask OnStoppingAsync() { }

    /// <summary>
    /// Handles logic to be executed when the parent of this entity changes.
    /// </summary>
    /// <param name="previous">The parent of this entity before the change. May be <see langword="null"/> if there was no parent.</param>
    /// <param name="current">The parent of this entity after the change. May be <see langword="null"/> if there is no parent.</param>
    protected virtual void OnParentChanged(Entity? previous, Entity? current)
    {
        Type type = GetType();

        if (previous is { Disposed: false })
        {
            previous.Remove(this, type);
        }

        if (current is { })
        {
            current.Add(this, type);
        }
    }

    /// <inheritdoc/>
    protected override void OnDisposing()
    {
        try
        {
            base.OnDisposing();

            KeyValuePair<Guid, Entity> item = new(ID, this);
            _entities.TryRemove(item);
        }
        catch (Exception ex)
        {
            Logger.LogException(ex, this);
        }
    }

    /// <summary>
    /// Attempts to retrieve the entity with the specified identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <param name="entity">
    /// When this method returns, contains the entity associated with the specified identifier, if found; otherwise,
    /// <see langword="null"/>. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the entity was found; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryFind(Guid id, [NotNullWhen(true)] out Entity? entity)
    {
        return _entities.TryGetValue(id, out entity);
    }

    /// <summary>
    /// Retrieves the entity of type <typeparamref name="T"/> with the specified unique identifier.
    /// </summary>
    /// <typeparam name="T">The type of entity to retrieve. Must derive from <see cref="Entity"/>.</typeparam>
    /// <param name="id">The unique identifier of the entity to retrieve.</param>
    /// <returns>The entity of type <typeparamref name="T"/> with the specified identifier.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no entity with the specified identifier exists in the scene.</exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the entity with the specified identifier exists but cannot be cast to type 
    /// <typeparamref name="T"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Find<T>(Guid id) where T : Entity
    {
        if (_entities.TryGetValue(id, out var entity))
        {
            return (T)entity;
        }

        ThrowKeyNotFoundException(id);
        return default;
    }

    private bool Clear([NotNullWhen(true)] out Repository? result)
    {
        Repository currentCollection = Volatile.Read(ref objects), oldCollection;
        do
        {
            oldCollection = currentCollection;
            if (oldCollection.Count == 0)
            {
                result = null;
                return false;
            }
            currentCollection = Interlocked.CompareExchange(ref objects, [], oldCollection);
        }
        while (!ReferenceEquals(oldCollection, currentCollection));
        result = currentCollection;
        return true;
    }

    private static void CompleteLifecycle(object? state)
    {
        TaskCompletionSource tcs = (TaskCompletionSource)state!;
#if NET8_0_OR_GREATER
        tcs.TrySetResult();
#else
        tcs.TrySetResult(null);
#endif
    }

#if NET6_0_OR_GREATER
    [StackTraceHidden]
#endif
    [DoesNotReturn, DebuggerStepThrough, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFoundException(Guid id)
    {
        throw new KeyNotFoundException($"No entity with ID {id} exists.");
    }

    /// <summary>Compares two values to determine equality.</summary>
    /// <param name="left">The value to compare with <paramref name="right" />.</param>
    /// <param name="right">The value to compare with <paramref name="left" />.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left" /> is equal to <paramref name="right" />; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Entity? left, Entity? right)
    {
        return Equals(left, right);
    }

    /// <summary>Compares two values to determine inequality.</summary>
    /// <returns>
    /// <see langword="true"/> if <paramref name="left" /> is not equal to <paramref name="right" />; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <inheritdoc cref="operator ==(Entity?, Entity?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Entity? left, Entity? right)
    {
        return !Equals(left, right);
    }

    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
        return ((IEnumerable<object>)objects.Values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)objects).GetEnumerator();
    }

    void ICollection<object>.Add(object item) => Add(item, item.GetType());

    void ICollection<object>.Clear() => Clear(out _);

    bool ICollection<object>.Remove(object item) => Remove(item, item.GetType());
}