using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProceduralGraph.Json;

internal sealed class RepositoryJsonConverter : DefaultJsonConverter<Repository>
{
    public override Repository? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        KeyValuePair<Type, object>[]? items = RentedArray.Acquire<KeyValuePair<Type, object>>();
        int count = 0;
        try
        {
            ThrowIfUnexpectedToken(ref reader, JsonTokenType.StartObject);
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
                    int index = count++;
                    RentedArray.Grow(ref items, count);
                    items[index] = new KeyValuePair<Type, object>(targetType, deserializedValue);
                }
            }

            return Repository.FromRange(items, 0, count);
        }
        finally
        {
            RentedArray.Return(ref items);
        }
    }

    public override void Write(Utf8JsonWriter writer, Repository value, JsonSerializerOptions options)
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
