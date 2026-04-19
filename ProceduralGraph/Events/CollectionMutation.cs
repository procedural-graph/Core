using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Events;

/// <summary>
/// Provides factory methods for creating instances of <see cref="CollectionMutation{TCollection, TArgs}"/>
/// </summary>
public static class CollectionMutation
{
    /// <inheritdoc cref="Insert{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Insert<TCollection, TArgs>(TCollection Updated, TArgs EventArgs)
    {
        return Insert(Updated, Updated, EventArgs);
    }

    /// <summary>Creates a new instance of <see cref="CollectionMutation{TCollection, TArgs}"/> representing an insertion mutation.</summary>
    /// <returns>A new instance of <see cref="CollectionMutation{TCollection, TArgs}"/> with the specified values.</returns>
    /// <inheritdoc cref="CollectionMutation{TCollection, TArgs}(TCollection, TCollection, TArgs, CollectionMutationType)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Insert<TCollection, TArgs>(TCollection Previous, TCollection Updated, TArgs EventArgs)
    {
        ThrowHelpers.ThrowIfNull(Previous);
        ThrowHelpers.ThrowIfNull(Updated);
        ThrowHelpers.ThrowIfNull(EventArgs);
        return new CollectionMutation<TCollection, TArgs?>(Previous, Updated, EventArgs, CollectionMutationType.Insert);
    }

    /// <inheritdoc cref="Delete{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Delete<TCollection, TArgs>(TCollection Updated, TArgs EventArgs)
    {
        return Delete(Updated, Updated, EventArgs);
    }

    /// <summary>Creates a new instance of <see cref="CollectionMutation{TCollection, TArgs}"/> representing a deletion mutation.</summary>
    /// <inheritdoc cref="Insert{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Delete<TCollection, TArgs>(TCollection Previous, TCollection Updated, TArgs EventArgs)
    {
        ThrowHelpers.ThrowIfNull(Previous);
        ThrowHelpers.ThrowIfNull(Updated);
        ThrowHelpers.ThrowIfNull(EventArgs);
        return new CollectionMutation<TCollection, TArgs?>(Previous, Updated, EventArgs, CollectionMutationType.Delete);
    }

    /// <inheritdoc cref="Update{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Update<TCollection, TArgs>(TCollection Updated, TArgs EventArgs)
    {
        return Update(Updated, Updated, EventArgs);
    }

    /// <summary>Creates a new instance of <see cref="CollectionMutation{TCollection, TArgs}"/> representing an update mutation.</summary>
    /// <inheritdoc cref="Insert{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Update<TCollection, TArgs>(TCollection Previous, TCollection Updated, TArgs EventArgs)
    {
        ThrowHelpers.ThrowIfNull(Previous);
        ThrowHelpers.ThrowIfNull(Updated);
        ThrowHelpers.ThrowIfNull(EventArgs);
        return new CollectionMutation<TCollection, TArgs?>(Previous, Updated, EventArgs, CollectionMutationType.Update);
    }

    /// <summary>Creates a new instance of <see cref="CollectionMutation{TCollection, TArgs}"/> representing a clear mutation.</summary>
    /// <inheritdoc cref="Insert{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Clear<TCollection, TArgs>(TCollection Previous)
    {
        ThrowHelpers.ThrowIfNull(Previous);
        return new CollectionMutation<TCollection, TArgs?>(Previous, default, default, CollectionMutationType.Reset);
    }

    /// <summary>Creates a new instance of <see cref="CollectionMutation{TCollection, TArgs}"/> representing a replacement mutation.</summary>
    /// <inheritdoc cref="Insert{TCollection, TArgs}(TCollection, TCollection, TArgs)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CollectionMutation<TCollection, TArgs?> Replace<TCollection, TArgs>(TCollection Previous, TCollection Updated)
    {
        ThrowHelpers.ThrowIfNull(Previous);
        ThrowHelpers.ThrowIfNull(Updated);
        return new CollectionMutation<TCollection, TArgs?>(Previous, Updated, default, CollectionMutationType.Reset);
    }
}

/// <summary>
/// Represents a change to a collection.
/// </summary>
/// <typeparam name="TCollection">The type of the items in the collection.</typeparam>
/// <typeparam name="TArgs">The type of the event arguments associated with the change.</typeparam>
/// <param name="Previous">The collection before the change occurred.</param>
/// <param name="Updated">The collection after the change occurred.</param>
/// <param name="EventArgs">The event arguments associated with the change.</param>
/// <param name="Type">The type of mutation that occurred in the collection.</param>
public readonly record struct CollectionMutation<TCollection, TArgs>(TCollection Previous, TCollection? Updated, TArgs? EventArgs, CollectionMutationType Type)
{
    /// <summary>Determines whether the mutation is an insertion.</summary>
    /// <remarks>An insertion occurs when a new item is added to the collection.</remarks>
    /// <returns><see langword="true"/> if the mutation is an insertion; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="CollectionMutation{TCollection, TArgs}(TCollection, TCollection, TArgs, CollectionMutationType)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsInsertion(out TCollection Previous, [NotNullWhen(true)] out TCollection? Updated, [NotNullWhen(true)] out TArgs? EventArgs)
    {
        Previous = this.Previous;
        Updated = this.Updated;
        EventArgs = this.EventArgs;
        return Type == CollectionMutationType.Insert;
    }

    /// <summary>Determines whether the mutation is a deletion.</summary>
    /// <remarks>A deletion occurs when an item is removed from the collection.</remarks>
    /// <returns><see langword="true"/> if the mutation is a deletion; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="CollectionMutation{TCollection, TArgs}(TCollection, TCollection, TArgs, CollectionMutationType)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDeletion(out TCollection Previous, [NotNullWhen(true)] out TCollection? Updated, [NotNullWhen(true)] out TArgs? EventArgs)
    {
        Previous = this.Previous;
        Updated = this.Updated;
        EventArgs = this.EventArgs;
        return Type == CollectionMutationType.Delete;
    }

    /// <summary>Determines whether the mutation is an update.</summary>
    /// <remarks>An update occurs when an item in the collection is modified.</remarks>
    /// <returns><see langword="true"/> if the mutation is an update; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="CollectionMutation{TCollection, TArgs}(TCollection, TCollection, TArgs, CollectionMutationType)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsUpdate(out TCollection Previous, [NotNullWhen(true)] out TCollection? Updated, [NotNullWhen(true)] out TArgs? EventArgs)
    {
        Previous = this.Previous;
        Updated = this.Updated;
        EventArgs = this.EventArgs;
        return Type == CollectionMutationType.Update;
    }

    /// <summary>Determines whether the mutation is a clear.</summary>
    /// <remarks>A clear occurs when all items are removed from the collection.</remarks>
    /// <returns><see langword="true"/> if the mutation is a clear; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="CollectionMutation{TCollection, TArgs}(TCollection, TCollection, TArgs, CollectionMutationType)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClear(out TCollection Previous)
    {
        Previous = this.Previous;
        return Type == CollectionMutationType.Reset && Updated is null;
    }

    /// <summary>Determines whether the mutation is a replacement.</summary>
    /// <remarks>A replacement occurs when a new instance is assigned to the collection.</remarks>
    /// <returns><see langword="true"/> if the mutation is a replacement; otherwise, <see langword="false"/>.</returns>
    /// <inheritdoc cref="CollectionMutation{TCollection, TArgs}(TCollection, TCollection, TArgs, CollectionMutationType)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsReplacement(out TCollection Previous, [NotNullWhen(true)] out TCollection? Updated)
    {
        Previous = this.Previous;
        Updated = this.Updated;
        return Type == CollectionMutationType.Reset && Updated is { };
    }
}