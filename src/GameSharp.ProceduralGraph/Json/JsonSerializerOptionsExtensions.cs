using ProceduralGraph;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GameSharp.ProceduralGraph.Json;

/// <summary>
/// Provides extension methods for configuring JSON serialization options.
/// </summary>
public static partial class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Adds a <see cref="ReadOnlyTypeLookupJsonConverter"/> to the <see cref="JsonSerializerOptions.Converters"/> collection.
    /// </summary>
    /// <param name="options">The <see cref="JsonSerializerOptions"/> to add the converter to.</param>
    /// <returns>The <see cref="JsonSerializerOptions"/> with the converter added.</returns>
    public static JsonSerializerOptions AddRepositoryConverter(this JsonSerializerOptions options)
    {
        ThrowHelpers.ThrowIfNull(options);
        options.Converters.Add(new ReadOnlyTypeLookupJsonConverter());
        return options;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DefaultJsonTypeInfoResolver CreateJsonTypeInfoResolver(Action<JsonTypeInfo> modifier)
    {
        DefaultJsonTypeInfoResolver resolver = new();
        resolver.Modifiers.Add(modifier);
        return resolver;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static JsonPolymorphismOptions CreateDefaultPolymorphismOptions() => new()
    {
        TypeDiscriminatorPropertyName = "$type",
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization
    };
}
