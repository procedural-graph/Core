using GameSharp.Collections;
using GameSharp.Collections.Immutable;
using GameSharp.Events;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.ProceduralGraph;

/// <summary>
/// Represents a base class for entities within a graph structure, providing identity, parent-child relationships,
/// and lifecycle management.
/// </summary>
/// <remarks>Initializes a new instance of the <see cref="Entity"/> class.</remarks>
/// <param name="logger">The logger instance used for recording diagnostic and operational messages.</param>
public class Entity(ILogger logger) : AsyncLifecycle, IEquatable<Entity>, ICollection<KeyValuePair<Type, object>>
{
    /// <summary>
    /// Provides data for the <see cref="Changed"/> event, indicating whether an item was added or removed from the entity.
    /// </summary>
    public readonly struct ChangeEventArgs
    {
        /// <summary>
        /// Indicates whether the change event represents an addition (<see langword="true"/>) or a removal (<see langword="false"/>) of an item.
        /// </summary>
        public bool Added { get; internal init; }

        /// <summary>
        /// Gets the item that was added or removed.
        /// </summary>
        public object Item { get; internal init; }

        /// <summary>
        /// Gets the type of the item that was added or removed.
        /// </summary>
        public Type Type { get; internal init; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ChangeEventArgs Add<T>(T item) where T : class => Add(item, typeof(T));
        internal static ChangeEventArgs Add(object item, Type type) => new()
        {
            Added = true,
            Item = item,
            Type = type
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ChangeEventArgs Remove<T>(T item) where T : class => Remove(item, typeof(T));
        internal static ChangeEventArgs Remove(object item, Type type) => new()
        {
            Added = false,
            Item = item,
            Type = type
        };
    }

    private static readonly ConcurrentDictionary<Guid, Entity> _entities = [];

    private readonly AsyncEventPublisher<Entity, ChangeEventArgs> _changed = AsyncEventPublisher.CreateQueue<Entity, ChangeEventArgs>(logger);
    /// <summary>
    /// Gets an asynchronous event that is triggered when the entity's contents change, such as when an item is added or removed.
    /// </summary>
    public AsyncEvent<Entity, ChangeEventArgs> Changed => _changed.Event;

    private readonly Guid _id;
    /// <summary>
    /// Gets the unique identifier for this graph entity.
    /// </summary>
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
    public Entity? Parent
    {
        get => Volatile.Read(ref _parent);
        set
        {
            Entity? oldValue = Interlocked.Exchange(ref _parent, value);

            if (Equals(oldValue, value) || Disposal.IsStarted)
            {
                return;
            }

            OnParentChanged(oldValue, value);
        }
    }

    private ImmutableTypeLookup _objects = [];

    /// <inheritdoc/>
    public int Count => _objects.Count;

    bool ICollection<KeyValuePair<Type, object>>.IsReadOnly => false;

    /// <inheritdoc cref="ReadOnlyTypeLookup.TryGetOne{T}(out T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne<T>([NotNullWhen(true)] out T? result) where T : class
    {
        result = default;
        return !Disposal.IsStarted && _objects.TryGetOne(out result);
    }

    /// <inheritdoc cref="ReadOnlyTypeLookup.GetOne{T}()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetOne<T>() where T : class
    {
        ObjectDisposedException.ThrowIf(Disposal.IsStarted, this);
        return _objects.GetOne<T>();
    }

    /// <inheritdoc cref="ReadOnlyTypeLookup.TryGetOne(Type, out object?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetOne([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out object? result)
    {
        result = default;
        return !Disposal.IsStarted && _objects.TryGetOne(type, out result);
    }

    /// <inheritdoc cref="ReadOnlyTypeLookup.GetOne(Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object GetOne(Type type) => _objects.GetOne(type);

    /// <inheritdoc cref="TypeLookup.Add{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add<T>(T item) where T : class
    {
        if (ImmutableTypeLookup.InterlockedUpdate(ref _objects, static (objs, item) => objs.Add(item), item))
        {
            _changed.Publish(this, ChangeEventArgs.Add(item));
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="TypeLookup.Add(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Add(object item, Type type)
    {
        if (ImmutableTypeLookup.InterlockedUpdate(ref _objects, static (objs, item, type) => objs.Add(item, type), item, type))
        {
            _changed.Publish(this, ChangeEventArgs.Add(item, type));
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="TypeLookup.Remove{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove<T>(T item) where T : class
    {
        if (ImmutableTypeLookup.InterlockedUpdate(ref _objects, static (objs, item) => objs.Remove(item), item))
        {
            _changed.Publish(this, ChangeEventArgs.Remove(item));
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="TypeLookup.Remove(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool Remove(object item, Type type)
    {
        if (ImmutableTypeLookup.InterlockedUpdate(ref _objects, static (objs, item, type) => objs.Remove(item, type), item, type))
        {
            _changed.Publish(this, ChangeEventArgs.Remove(item, type));
            return true;
        }

        return false;
    }

    /// <inheritdoc cref="ReadOnlyTypeLookup.Contains(object, Type)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(object item, Type type) => _objects.Contains(item, type);

    /// <inheritdoc cref="ReadOnlyTypeLookup.Contains{T}(T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains<T>(T item) where T : class => _objects.Contains(item);

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

    /// <inheritdoc/>
    protected override async Task OnStartingAsync()
    {
        ref Task task = ref Rent(16, out Task[] taskArray);
        int count = 0;

        foreach (AsyncLifecycle lifecycle in _objects.GetAll<AsyncLifecycle>())
        {
            int index = count++;

            if (count > taskArray.Length)
            {
                task = ref Grow(count, ref taskArray);
            }

            Unsafe.Add(ref task, index) = lifecycle.StartAsync(StoppingToken);
        }

        Task wait = WaitAndReturnAsync(taskArray, count);
        await wait.ConfigureAwait(false);

        _entities[_id] = this;
    }

    /// <inheritdoc/>
    protected override async Task OnStoppingAsync()
    {
        ((ICollection<KeyValuePair<Guid, Entity>>)_entities).Remove(new KeyValuePair<Guid, Entity>(ID, this));

        Task stopping = base.OnStoppingAsync();
        await stopping.ConfigureAwait(false);

        ref Task task = ref Rent(16, out Task[] taskArray);
        int count = 0;

        foreach (AsyncLifecycle lifecycle in _objects.GetAll<AsyncLifecycle>())
        {
            int index = count++;

            if (count > taskArray.Length)
            {
                task = ref Grow(count, ref taskArray);
            }

            Unsafe.Add(ref task, index) = lifecycle.StopAsync();
        }

        Task wait = WaitAndReturnAsync(taskArray, count);
        await wait.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override async Task OnDisposingAsync()
    {
        Task disposing = base.OnDisposingAsync();
        await disposing.ConfigureAwait(false);

        int count = 0;
        ref Task task = ref Rent(16, out Task[] taskArray);

        foreach (IAsyncDisposable asyncDisposable in _objects.GetAll<IAsyncDisposable>())
        {
            ValueTask disposal = asyncDisposable.DisposeAsync();

            if (disposal.IsCompletedSuccessfully)
            {
                continue;
            }

            int index = count++;

            if (count > taskArray.Length)
            {
                task = ref Grow(count, ref taskArray);
            }

            Unsafe.Add(ref task, index) = disposal.AsTask();
        }

        Task asyncDisposal = Task.WhenAll(taskArray.AsSpan(0, count));
        await asyncDisposal.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        count = 0;

        foreach (IDisposable disposable in _objects.GetAll<IDisposable>())
        {
            Task disposal = Task.Run(disposable.Dispose);

            int index = count++;

            if (count > taskArray.Length)
            {
                task = ref Grow(count, ref taskArray);
            }

            Unsafe.Add(ref task, index) = disposal;
        }

        Task syncDisposal = Task.WhenAll(taskArray.AsSpan(0, count));
        await syncDisposal.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        ArrayPool<Task>.Shared.Return(taskArray, clearArray: true);

        if (!asyncDisposal.IsCompletedSuccessfully)
        {
            if (syncDisposal.IsCompletedSuccessfully)
            {
                Throw(asyncDisposal.Exception!);
            }

            if (asyncDisposal.Exception is AggregateException aggEx1)
            {
                if (syncDisposal.Exception is AggregateException aggEx2)
                {
                    throw new AggregateException([.. aggEx1.InnerExceptions, .. aggEx2.InnerExceptions]);
                }

                throw new AggregateException([.. aggEx1.InnerExceptions, syncDisposal.Exception!]);
            }
        }

        if (!syncDisposal.IsCompletedSuccessfully)
        {
            Throw(syncDisposal.Exception!);
        }
    }

    /// <summary>
    /// Handles logic to be executed when the parent of this entity changes.
    /// </summary>
    /// <param name="previous">The parent of this entity before the change. May be <see langword="null"/> if there was no parent.</param>
    /// <param name="current">The parent of this entity after the change. May be <see langword="null"/> if there is no parent.</param>
    protected virtual void OnParentChanged(Entity? previous, Entity? current)
    {
        Type type = GetType();

        if (previous is { Disposal.IsStarted: false })
        {
            previous.Remove(this, type);
        }

        if (current is { })
        {
            current.Add(this, type);
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
        if (!_entities.TryGetValue(id, out var entity))
        {
            Throw(new KeyNotFoundException($"No entity with ID {id} exists."));
        }

        return (T)entity;
    }

    private static async Task WaitAndReturnAsync(Task[] taskArray, int count)
    {
        Task wait = Task.WhenAll(taskArray.AsSpan(0, count));
        await wait.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        ArrayPool<Task>.Shared.Return(taskArray, clearArray: true);
        if (!wait.IsCompletedSuccessfully)
        {
            Throw(wait.Exception!);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref T Rent<T>(int minimumLength, out T[] taskArray)
    {
        taskArray = ArrayPool<T>.Shared.Rent(minimumLength);
        return ref MemoryMarshal.GetArrayDataReference(taskArray);
    }

    private static ref T Grow<T>(int minimumLength, scoped ref T[] array)
    {
        int capacity = array.Length;

        do
        {
            capacity += capacity >> 1;
        }
        while (capacity < minimumLength);

        T[] oldArray = array;
        ref T item = ref Rent(capacity, out array);
        Array.Copy(oldArray, array, oldArray.Length);

        ArrayPool<T>.Shared.Return(oldArray, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

        return ref item;
    }

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void Throw(Exception exception)
    {
        throw exception;
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

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator()
    {
        return ((IEnumerable<KeyValuePair<Type, object>>)_objects).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_objects).GetEnumerator();
    }

    void ICollection<KeyValuePair<Type, object>>.Add(KeyValuePair<Type, object> item)
    {
        Add(item.Value, item.Key);
    }

    bool ICollection<KeyValuePair<Type, object>>.Remove(KeyValuePair<Type, object> item)
    {
        return Remove(item.Value, item.Key);
    }

    bool ICollection<KeyValuePair<Type, object>>.Contains(KeyValuePair<Type, object> item)
    {
        return Contains(item.Value, item.Key);
    }

    void ICollection<KeyValuePair<Type, object>>.CopyTo(KeyValuePair<Type, object>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<Type, object>>)_objects).CopyTo(array, arrayIndex);
    }

    void ICollection<KeyValuePair<Type, object>>.Clear()
    {
        Throw(new NotSupportedException("Clearing all items from an Entity is not supported."));
    }
}