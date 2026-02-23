namespace ProceduralGraph.Collections;

/// <summary>
/// Specifies the type of change that has occurred to an item.
/// </summary>
public enum ItemChangeType : sbyte
{
    /// <summary>
    /// Indicates that the item has been added.
    /// </summary>
    Added = +1,
    /// <summary>
    /// Indicates that the item has been removed.
    /// </summary>
    Removed = -1
}
