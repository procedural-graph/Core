using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace ProceduralGraph.Generic.Converters;

public partial class GraphConverterRegistrar
{
    private readonly Dictionary<Type, HashSet<IGraphConverter>> _convertersByType = [];
    private readonly HashSet<IGraphConverter> _statefulConverters = [];
    private readonly HashSet<IGraphConverter> _statelessConverters = [];

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

    private static KeyValuePair<Type, ImmutableSortedSet<IGraphConverter>> ToImmutableConverterSet(KeyValuePair<Type, HashSet<IGraphConverter>> kvp)
    {
        return new KeyValuePair<Type, ImmutableSortedSet<IGraphConverter>>(kvp.Key, [.. kvp.Value]);
    }
}
