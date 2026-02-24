using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph;

/// <summary>
/// Provides functionality for finding graph converters that can convert various types of objects.
/// </summary>
public interface IGraphConverterProvider
{
    /// <summary>
    /// Attempts to find a converter that can convert the specified object.
    /// </summary>
    /// <param name="obj">The object to be converted. Can be <see langword="null"/>.</param>
    /// <param name="result">When this method returns, contains the first converter that can convert the specified object, if found;
    /// otherwise, <see langword="null"/>. This parameter is passed uninitialized.</param>
    /// <returns><see langword="true"/> if a suitable converter is found; otherwise, <see langword="false"/>.</returns>
    bool TryFind([NotNullWhen(true)] object? obj, [NotNullWhen(true)] out IGraphConverter? result);

    /// <summary>
    /// Searches for a converter that supports the given object.
    /// </summary>
    /// <param name="obj">The object for which to find a compatible graph converter. Cannot be <see langword="null"/>.</param>
    /// <returns>The first graph converter that supports the specified object.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no compatible graph converter is found for the specified object.</exception>
    IGraphConverter Find(object obj);
}