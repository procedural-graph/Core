namespace ProceduralGraph.Events;

/// <summary>
/// Defines the type of mutation that has occurred in a collection.
/// </summary>
public enum CollectionMutationType : byte
{
    /// <summary>
    /// Indicates that an item in the collection has been replaced or modified.
    /// </summary>
    Update,
    /// <summary>
    /// Indicates that an item has been added to the collection.
    /// </summary>
    Insert,
    /// <summary>
    /// Indicates that an item has been removed from the collection.
    /// </summary>
    Delete,
    /// <summary>
    /// Indicates that the content of the collection has changed in it's entirety.
    /// </summary>
    Reset
}
