namespace ProceduralGraph.Collections;

/// <summary>
/// Represents event data for a change to an item, including the affected item and the type of change that occurred.
/// </summary>
/// <typeparam name="T">The type of the item associated with the event.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="ItemEventArgs{T}"/> structure with the specified item and change type.
/// </remarks>
/// <param name="Item">The item associated with the event. This parameter cannot be <see langword="null"/>.</param>
/// <param name="ChangeType">The type of change that occurred to the item.</param>
public readonly record struct ItemEventArgs<T>(T Item, ItemChangeType ChangeType);