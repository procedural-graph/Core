using System.Collections.Immutable;

namespace GameSharp.Collections;

/// <summary>
/// Represents type information for a registered type in the <see cref="TypeRegistry"/>.
/// </summary>
public interface ITypeInfo
{
    /// <summary>
    /// Gets the <see cref="Type"/> represented by this type info.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the unique ID of this type.
    /// </summary>
    int ID { get; }

    /// <summary>
    /// Gets an immutable array of IDs representing the derived types of this type, including the type itself.
    /// </summary>
    /// <remarks>The values are sorted in ascending order and are unique.</remarks>
    ImmutableArray<int> DerivedTypeIDs { get; }
}
