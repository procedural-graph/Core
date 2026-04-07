using System;
using System.Collections.Generic;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Provides extension methods for building and managing graph converter providers using collections of types and
/// dependency resolution services.
/// </summary>
public static class GraphConverterProviderExtensions
{
    /// <summary>
    /// Builds and returns a new instance of the graph converter provider using the types in the collection.
    /// </summary>
    /// <param name="types">The collection of types to use for building the graph converter provider.</param>
    /// <param name="serviceProvider">The service provider to use for resolving dependencies when building the graph converter provider.</param>
    /// <returns>A <see cref="GraphConverterProvider"/> instance containing all registered graph converters.</returns>
    public static GraphConverterProvider BuildConverterProvider(this ICollection<Type> types, IServiceProvider serviceProvider)
    {
        ThrowHelpers.ThrowIfNull(types, nameof(types));
        ThrowHelpers.ThrowIfNull(serviceProvider, nameof(serviceProvider));
        GraphConverterProvider.Builder builder = GraphConverterProvider.CreateBuilder(serviceProvider, types);
        Span<Range> ranges = stackalloc Range[types.Count];
        int length = builder.CalculateConverterRangesAndLength(ranges);
        Dictionary<Type, ReadOnlyMemory<IGraphConverter>> convertersByType = new(types.Count);
#if NET6_0_OR_GREATER
        IGraphConverter[] allConverters = GC.AllocateUninitializedArray<IGraphConverter>(length);
#else
        IGraphConverter[] allConverters = new IGraphConverter[length];
#endif
        int index = builder.FillConverterArrayAndDictionary(ranges, allConverters, convertersByType);
        return new GraphConverterProvider(convertersByType, allConverters.AsMemory(index, builder.StatefulConverters.Count));
    }
}
