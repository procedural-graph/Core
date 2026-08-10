using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameSharp.ProceduralGraph.Json;

/// <inheritdoc/>
public abstract class DefaultJsonConverter<T> : JsonConverter<T>
{
    /// <summary>
    /// Defines a contract for deserializing a member from JSON using a specified method and member name.
    /// </summary>
    protected interface IMemberDeserializer
    {
        /// <summary>
        /// An unsafe delegate that deserializes a member of type <typeparamref name="T"/> from JSON using the provided <see cref="Utf8JsonReader"/>,
        /// </summary>
        unsafe delegate*<ref Utf8JsonReader, T, JsonSerializerOptions, void> Method { get; }

        /// <summary>
        /// Gets the name of the member to be deserialized, represented as a UTF-8 encoded byte span.
        /// </summary>
        ReadOnlySpan<byte> Name { get; }
    }

    /// <summary>
    /// Handles a JSON property that does not map to any member of the target type during deserialization.
    /// </summary>
    /// <param name="reader">
    /// A reference to the current <see cref="Utf8JsonReader"/> positioned at the unmapped property. The reader will be advanced 
    /// past the unmapped property after the method completes.
    /// </param>
    /// <param name="options">
    /// The <see cref="JsonSerializerOptions"/> used to control deserialization behavior, including how unmapped members are 
    /// handled.
    /// </param>
    /// <exception cref="JsonException">
    /// Thrown if an unmapped member is encountered and the <see cref="JsonUnmappedMemberHandling"/> is set to 
    /// <see cref="JsonUnmappedMemberHandling.Disallow"/>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void HandleUnmappedMember(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        if (options.UnmappedMemberHandling == JsonUnmappedMemberHandling.Disallow)
        {
            ThrowJsonException($"Unexpected JSON property '{reader.GetString()}' at position {reader.TokenStartIndex}.");
        }

        reader.Read();
        reader.Skip();
    }

    /// <summary>
    /// Ensures the current token in the specified reader is equal to the specified type.
    /// </summary>
    /// <param name="reader">The JSON reader to validate.</param>
    /// <param name="expected">The expected token type that the reader's current token should match.</param>
    /// <exception cref="JsonException">Thrown if the current token in the specified reader does not equal <paramref name="expected"/>.</exception>
    [StackTraceHidden]
    protected static void ThrowIfUnexpectedToken(ref Utf8JsonReader reader, JsonTokenType expected)
    {
        JsonTokenType actual = reader.TokenType;
        if (actual != expected)
        {
            ThrowJsonException($"Expected {GetEnumName(expected)}, found {GetEnumName(actual)}.");
        }
    }

    /// <summary>
    /// Attempts to deserialize a property from the current position of the JSON reader and updates the provided setters
    /// collection by removing the matched setter.
    /// </summary>
    /// <typeparam name="TDeserializer">
    /// The type of the member deserializer, which must be a struct implementing the <see cref="IMemberDeserializer"/> 
    /// interface.
    /// /typeparam>
    /// <param name="reader">A reference to the <see cref="Utf8JsonReader"/> positioned at the property to be deserialized.</param>
    /// <param name="setters">
    /// A reference to a span of member deserializers representing the remaining properties to be deserialized. The
    /// matched setter will be removed from this span if deserialization succeeds.
    /// </param>
    /// <param name="value">The target object instance to populate with the deserialized value.</param>
    /// <param name="options">The options to use for JSON deserialization, such as converters and formatting settings.</param>
    /// <returns><see langword="true"/> if a matching property was found and deserialized; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static unsafe bool DeserializeTruncate<TDeserializer>(
        ref Utf8JsonReader reader, 
        ref Span<TDeserializer> setters, 
        T value, 
        JsonSerializerOptions options) 
        where TDeserializer : struct, IMemberDeserializer
    {
        foreach (ref TDeserializer setter in setters)
        {
            if (!reader.ValueTextEquals(setter.Name))
            {
                continue;
            }

            setter.Method(ref reader, value, options);

            int lastIndex = setters.Length - 1;
            ref TDeserializer lastElement = ref setters[lastIndex];

            (setter, lastElement) = (lastElement, setter);
            setters = setters[..lastIndex];

            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates strongly-typed getter and setter delegates for a specified property or field of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property or field to access.</typeparam>
    /// <param name="memberName">The name of the property or field for which to create accessors.</param>
    /// <returns>A tuple containing a getter delegate and a setter delegate for the specified property or field.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no property or field with the specified name exists on type <typeparamref name="T"/>.</exception>
    protected static (Func<T, TProperty> getter, Action<T, TProperty> setter) CreateAccessors<TProperty>(string memberName)
    {
        const BindingFlags BindingAttributes = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        Type type = typeof(T);

        if (type.GetProperty(memberName, BindingAttributes) is { } propertyInfo)
        {
            if (propertyInfo.GetGetMethod(nonPublic: true) is { } getMethod && propertyInfo.GetSetMethod(nonPublic: true) is { } setMethod)
            {
                return (CreateDelegate<TProperty, Func<T, TProperty>>(getMethod), CreateDelegate<TProperty, Action<T, TProperty>>(setMethod));
            }
        }
        else if (type.GetField(memberName, BindingAttributes) is { } fieldInfo)
        {
            return (o => (TProperty)fieldInfo.GetValue(o)!, (o, v) => fieldInfo.SetValue(o, v));
        }

        throw new InvalidOperationException($"No property or field named '{memberName}' found on type '{type.FullName}'.");
    }

    private static TDelegate CreateDelegate<TProperty, TDelegate>(MethodInfo methodInfo) where TDelegate : Delegate
    {
        return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate), methodInfo);
    }

    [StackTraceHidden]
    [DoesNotReturn, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowJsonException(string message)
    {
        throw new JsonException(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string? GetEnumName<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        return Enum.GetName(value);
    }
}
