using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProceduralGraph.Generic.Converters
{
    public partial class GraphConverterRegistrar
    {
        private readonly Dictionary<Type, HashSet<IGraphConverter>> _convertersByType = new Dictionary<Type, HashSet<IGraphConverter>>();
        private readonly HashSet<IGraphConverter> _statefulConverters = new HashSet<IGraphConverter>();
        private readonly HashSet<IGraphConverter> _statelessConverters = new HashSet<IGraphConverter>();

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

        private static KeyValuePair<Type, ImmutableSortedSet<IGraphConverter>> ToImmutableConverterSet(KeyValuePair<Type, HashSet<IGraphConverter>> kvp)
        {
            return new KeyValuePair<Type, ImmutableSortedSet<IGraphConverter>>(kvp.Key, kvp.Value.ToImmutableSortedSet());
        }
    }
}
