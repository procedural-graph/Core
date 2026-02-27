using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
#if NET6_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

namespace ProceduralGraph.Generic.Converters;

/// <summary>
/// Represents a collection for registering and managing graph converters used to build a graph converter provider.
/// </summary>
public sealed class GraphConverterRegistrar : ICollection<IGraphConverter>
{
    /// <summary>
    /// Enumerates the collection of graph converters registered in a <see cref="GraphConverterRegistrar"/> instance.
    /// </summary>
    public readonly struct Enumerator : IEnumerator<IGraphConverter>
    {
        private readonly GraphConverterRegistrar _registrar;
        private readonly HashSet<IGraphConverter>.Enumerator _statefulEnumerator;
        private readonly HashSet<IGraphConverter>.Enumerator _statelessEnumerator;
        private readonly int _version;

        internal Enumerator(GraphConverterRegistrar registrar)
        {
            _version = registrar._version;
            _registrar = registrar;
            _statefulEnumerator = registrar._statefulConverters.GetEnumerator();
            _statelessEnumerator = registrar._statelessConverters.GetEnumerator();
        }

        /// <inheritdoc/>
        public IGraphConverter Current => _statefulEnumerator.Current;
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public void Dispose()
        {
            _statefulEnumerator.Dispose();
            _statelessEnumerator.Dispose();
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_version != _registrar._version)
            {
                throw new InvalidOperationException("The collection was modified after the enumerator was created.");
            }

            if (_statefulEnumerator.MoveNext())
            {
                return true;
            }

            return _statelessEnumerator.MoveNext();
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException($"Reset is not supported for {GetType().FullName}. Create a new enumerator instead.");
        }
    }

    private readonly Dictionary<Type, HashSet<IGraphConverter>> _convertersByType = [];
    private readonly HashSet<IGraphConverter> _statefulConverters = [];
    private readonly HashSet<IGraphConverter> _statelessConverters = [];

    private int _version;

    /// <inheritdoc/>
    public int Count => _statefulConverters.Count + _statelessConverters.Count;

    bool ICollection<IGraphConverter>.IsReadOnly => false;

    private static KeyValuePair<Type, ImmutableSortedSet<IGraphConverter>> ToImmutableConverterSet(KeyValuePair<Type, HashSet<IGraphConverter>> kvp)
    {
        return new KeyValuePair<Type, ImmutableSortedSet<IGraphConverter>>(kvp.Key, [.. kvp.Value]);
    }

    /// <summary>
    /// Builds and returns a new instance of the graph converter provider using the current set of registered
    /// converters.
    /// </summary>
    /// <returns>A <see cref="GraphConverterProvider"/> instance containing all registered graph converters.</returns>
    public GraphConverterProvider BuildConverterProvider()
    {
        Span<Range> ranges = stackalloc Range[_convertersByType.Count];
        int length = CalculateConverterRangesAndLength(ranges);
#if NET6_0_OR_GREATER
        IGraphConverter[] allConverters = GC.AllocateUninitializedArray<IGraphConverter>(length);
        Dictionary<Type, ReadOnlyMemory<IGraphConverter>> convertersByType = new(_convertersByType.Count);
#else
        Dictionary<Type, ReadOnlyMemory<IGraphConverter>> convertersByType = new Dictionary<Type, ReadOnlyMemory<IGraphConverter>>(_convertersByType.Count);
        IGraphConverter[] allConverters = new IGraphConverter[length];
#endif
        int index = FillConverterArrayAndDictionary(ranges, allConverters, convertersByType);
        _statefulConverters.CopyTo(allConverters, index);
        return new GraphConverterProvider(convertersByType, allConverters.AsMemory(index, _statefulConverters.Count));
    }

    /// <inheritdoc/>
    public void Add(IGraphConverter converter)
    {
        _version++;

        IReadOnlyCollection<Type> supportedTypes = converter.SupportedTypes;

        if (supportedTypes.Count == 0)
        {
            _statefulConverters.Add(converter);
            return;
        }

        if (!_statelessConverters.Add(converter))
        {
            return;
        }

        foreach (Type supportedType in supportedTypes)
        {
            RegisterConverterToType(converter, supportedType);
        }
    }

    /// <inheritdoc cref="Add(IGraphConverter)"/>
    public void Add(GraphConverter converter)
    {
        _version++;

        ImmutableArray<Type> supportedTypes = converter.SupportedTypes;

        if (supportedTypes.IsEmpty)
        {
            _statefulConverters.Add(converter);
            return;
        }

        if (!_statelessConverters.Add(converter))
        {
            return;
        }

        for (var i = 0; i < supportedTypes.Length; i++)
        {
            RegisterConverterToType(converter, supportedTypes[i]);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _convertersByType.Clear();
        _statefulConverters.Clear();
        _statelessConverters.Clear();
    }

    /// <inheritdoc/>
    public bool Contains(IGraphConverter item)
    {
        return _statefulConverters.Contains(item) || _statelessConverters.Contains(item);
    }

    /// <inheritdoc/>
    public void CopyTo(IGraphConverter[] array, int arrayIndex)
    {
#if NET5_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(array);
#else
        if (array is null)
        {
            throw new ArgumentNullException(nameof(array));
        }
#endif
        if (arrayIndex < 0 || arrayIndex >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        _statelessConverters.CopyTo(array, arrayIndex);
        _statefulConverters.CopyTo(array, arrayIndex + _statelessConverters.Count);
    }

    /// <inheritdoc cref="IEnumerable{IGraphConverter}.GetEnumerator"/>
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    /// <inheritdoc/>
    public bool Remove(IGraphConverter item)
    {
        IReadOnlyCollection<Type> supportedTypes = item.SupportedTypes;

        if (supportedTypes.Count == 0)
        {
            if (_statefulConverters.Remove(item))
            {
                _version++;
                return true;
            }

            return false;
        }

        if (!_statelessConverters.Remove(item))
        {
            return false;
        }

        _version++;

        foreach (Type type in supportedTypes)
        {
            UnregisterConverterForType(item, type);
        }

        return true;
    }

    /// <inheritdoc cref="Remove(IGraphConverter)"/>
    public bool Remove(GraphConverter item)
    {
        ImmutableArray<Type> supportedTypes = item.SupportedTypes;

        if (supportedTypes.IsEmpty)
        {
            if (_statefulConverters.Remove(item))
            {
                _version++;
                return true;
            }

            return false;
        }

        if (!_statelessConverters.Remove(item))
        {
            return false;
        }

        _version++;

        for (int i = 0; i < supportedTypes.Length; i++)
        {
            UnregisterConverterForType(item, supportedTypes[i]);
        }

        return true;
    }

#if NET5_0_OR_GREATER
    private void RegisterConverterToType(IGraphConverter converter, Type type)
    {
        ref HashSet<IGraphConverter>? set = ref CollectionsMarshal.GetValueRefOrAddDefault(_convertersByType, type, out bool exists);
        if (exists)
        {
            set!.Add(converter);
        }
        else
        {
            set = [converter];
        }
    }
#else
    private void RegisterConverterToType(IGraphConverter converter, Type type)
    {
        if (_convertersByType.TryGetValue(type, out HashSet<IGraphConverter>? existingConverters))
        {
            existingConverters.Add(converter);
        }
        else
        {
            _convertersByType.Add(type, new HashSet<IGraphConverter> { converter });
        }
    }
#endif

    private int FillConverterArrayAndDictionary(Span<Range> ranges, IGraphConverter[] allConverters, Dictionary<Type, ReadOnlyMemory<IGraphConverter>> convertersByType)
    {
        int index = 0;
        Dictionary<Type, HashSet<IGraphConverter>>.Enumerator enumerator = _convertersByType.GetEnumerator();
        while (enumerator.MoveNext())
        {
#if NETFRAMEWORK
            Type type = enumerator.Current.Key;
            HashSet<IGraphConverter> converters = enumerator.Current.Value;
#else
            (Type type, HashSet<IGraphConverter> converters) = enumerator.Current;
#endif
            int count = converters.Count;
            converters.CopyTo(allConverters, index);
            Array.Sort(allConverters, index, count);
#if NETFRAMEWORK
            (int offset, int length) = ranges[index].GetOffsetAndLength(allConverters.Length);
            convertersByType.Add(type, allConverters.AsMemory(offset, length));
#else
            convertersByType.Add(type, allConverters.AsMemory(ranges[index]));
#endif
            index += count;
        }
        return index;
    }

    private int CalculateConverterRangesAndLength(Span<Range> ranges)
    {
        int length = 0, index = 0, offset = 0;
        using Dictionary<Type, HashSet<IGraphConverter>>.ValueCollection.Enumerator enumerator = _convertersByType.Values.GetEnumerator();
        while (enumerator.MoveNext())
        {
            length += enumerator.Current.Count;
            ranges[index++] = offset..length;
            offset = length;
        }
        length += _statefulConverters.Count;
        return length;
    }

    private void UnregisterConverterForType(IGraphConverter converter, Type type)
    {
        if (!_convertersByType.TryGetValue(type, out HashSet<IGraphConverter>? converters))
        {
            return;
        }

        converters.Remove(converter);
        if (converters.Count == 0)
        {
            _convertersByType.Remove(type);
        }
    }

    IEnumerator<IGraphConverter> IEnumerable<IGraphConverter>.GetEnumerator()
    {
        return GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
