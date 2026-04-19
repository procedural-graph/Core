using System.Collections.Generic;

namespace ProceduralGraph.Collections;

/// <inheritdoc cref="ICollection{T}"/>
public interface IBigCollection<T> : ICollection<T>
{
    /// <inheritdoc cref="ICollection{T}.Count"/>
    new long Count { get; }

#if !NETFRAMEWORK
    int ICollection<T>.Count => checked((int)Count);
#endif
}
