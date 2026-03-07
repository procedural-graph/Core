using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Provides a mechanism for locating and retrieving graph converters capable of handling specific object types.
/// </summary>
public sealed class GraphConverterProvider : IGraphConverterProvider
{
    internal readonly ref struct Builder
    {
        public Dictionary<Type, HashSet<IGraphConverter>> ConvertersByType { get; init; }
        public HashSet<IGraphConverter> StatefulConverters { get; init; }
        public HashSet<IGraphConverter> StatelessConverters { get; init; }

        public int CalculateConverterRangesAndLength(Span<Range> ranges)
        {
            int length = 0, index = 0, offset = 0;
            foreach (HashSet<IGraphConverter> converters in ConvertersByType.Values)
            {
                length += converters.Count;
                ranges[index++] = offset..length;
                offset = length;
            }
            length += StatefulConverters.Count;
            return length;
        }

        public int FillConverterArrayAndDictionary(
        Span<Range> ranges,
        IGraphConverter[] allConverters,
        Dictionary<Type, ReadOnlyMemory<IGraphConverter>> converterIndicesByType)
        {
            int index = 0;
            foreach (KeyValuePair<Type, HashSet<IGraphConverter>> kvp in ConvertersByType)
            {
                int count = kvp.Value.Count;
                kvp.Value.CopyTo(allConverters, index);
                Array.Sort(allConverters, index, count);
                (int offset, int length) = ranges[index].GetOffsetAndLength(allConverters.Length);
                converterIndicesByType.Add(kvp.Key, allConverters.AsMemory(offset, length));
                index += count;
            }
            return index;
        }
    }

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

    internal static Builder CreateBuilder(IServiceProvider serviceProvider, ICollection<Type> types)
    {
        Builder builder = new()
        {
            ConvertersByType = [],
            StatefulConverters = [],
            StatelessConverters = []
        };

        foreach (Type converterType in types)
        {
            IGraphConverter converter = (IGraphConverter)ActivatorUtilities.CreateInstance(serviceProvider, converterType);

            if (converter.SupportedTypes.Count == 0)
            {
                builder.StatefulConverters.Add(converter);
                continue;
            }

            builder.StatelessConverters.Add(converter);

            foreach (Type supportedType in converter.SupportedTypes)
            {
#if NET6_0_OR_GREATER
                ref HashSet<IGraphConverter>? set = ref CollectionsMarshal.GetValueRefOrAddDefault(builder.ConvertersByType, supportedType, out bool exists);
                if (exists)
                {
                    set!.Add(converter);
                    continue;
                }
                set = [converter];
#else
                if (builder.ConvertersByType.TryGetValue(supportedType, out HashSet<IGraphConverter>? existingConverters))
                {
                    existingConverters.Add(converter);
                    continue;
                }
                builder.ConvertersByType.Add(supportedType, [converter]);
#endif
            }
        }

        return builder;
    }
}
