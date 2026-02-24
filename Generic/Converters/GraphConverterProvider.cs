using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Provides a mechanism for locating and retrieving graph converters capable of handling specific object types.
/// </summary>
public sealed class GraphConverterProvider : IGraphConverterProvider
{
    private readonly FrozenDictionary<Type, ReadOnlyMemory<IGraphConverter>> _convertersByType;
    private readonly ReadOnlyMemory<IGraphConverter> _statefulConverters;

    internal GraphConverterProvider(Dictionary<Type, ReadOnlyMemory<IGraphConverter>> convertersByType, ReadOnlyMemory<IGraphConverter> statefulConverters)
    {
        _convertersByType = convertersByType.ToFrozenDictionary();
        _statefulConverters = statefulConverters;
    }

    /// <inheritdoc/>
    public IGraphConverter Find(object obj)
    {
        if (TryFind(obj, out IGraphConverter? result))
        {
            return result;
        }

        throw new InvalidOperationException($"No converter found for object of type {obj.GetType()}.");
    }

    /// <inheritdoc/>
    public bool TryFind([NotNullWhen(true)] object? obj, [NotNullWhen(true)] out IGraphConverter? result)
    {
        if (obj is { } && (TryFindStateless(obj, out result) || TryFindStateful(obj, out result)))
        {
            return true;
        }

        result = null;
        return false;
    }

    private bool TryFindStateless(object obj, [NotNullWhen(true)] out IGraphConverter? result)
    {
        if (_convertersByType.TryGetValue(obj.GetType(), out ReadOnlyMemory<IGraphConverter> converters) && TryConvert(obj, converters.Span, out result))
        {
            return true; 
        }

        result = null;
        return false;
    }

    private bool TryFindStateful(object obj, [NotNullWhen(true)] out IGraphConverter? result)
    {
        if (_statefulConverters.IsEmpty)
        {
            result = null;
            return false;
        }

        return TryConvert(obj, _statefulConverters.Span, out result);
    }

    private static bool TryConvert(object obj, ReadOnlySpan<IGraphConverter> converters, [NotNullWhen(true)] out IGraphConverter? result)
    {
        int index = 0;
        while (index < converters.Length)
        {
            IGraphConverter converter = converters[index++];
            if (converter.CanConvert(obj))
            {
                result = converter;
                return true;
            }
        }

        result = null;
        return false;
    }
}
