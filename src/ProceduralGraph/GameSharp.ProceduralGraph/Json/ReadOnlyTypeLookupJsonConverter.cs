using GameSharp.Collections.Immutable;
using ProceduralGraph;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace GameSharp.ProceduralGraph.Json;

internal sealed class ImmutableTypeLookupJsonConverter : DefaultJsonConverter<ImmutableTypeLookup>
{
    public override ImmutableTypeLookup? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        ThrowIfUnexpectedToken(ref reader, JsonTokenType.StartObject);

        ImmutableTypeLookup.Builder builder = ImmutableTypeLookup.CreateBuilder();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            ThrowIfUnexpectedToken(ref reader, JsonTokenType.PropertyName);

            string? typeName = reader.GetString();
            reader.Read();
            if (string.IsNullOrWhiteSpace(typeName))
            {
                throw new JsonException("Type name cannot be null or empty.");
            }

            if (Type.GetType(typeName) is not Type targetType)
            {
                throw new JsonException($"Cannot resolve type from name '{typeName}'.");
            }

            if (JsonSerializer.Deserialize(ref reader, targetType, options) is { } deserializedValue)
            {
                builder.Add(deserializedValue, targetType);
            }
        }

        return builder.ToImmutable();
    }

    public override void Write(Utf8JsonWriter writer, ImmutableTypeLookup value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (KeyValuePair<Type, object> kvp in value)
        {
            string? typeName = kvp.Key.AssemblyQualifiedName;
            ThrowHelpers.ThrowIf(typeName is null, $"Cannot resolve name for type {kvp.Key}.");
            writer.WritePropertyName(typeName);
            JsonSerializer.Serialize(writer, kvp.Value, kvp.Key, options);
        }

        writer.WriteEndObject();
    }
}
