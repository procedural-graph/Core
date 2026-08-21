using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections;

internal abstract class TypeRegistryProvider
{
    public abstract TypeRegistry GetOrAdd(Type type);

    public abstract bool TryGet(Type type, [NotNullWhen(true)] out TypeRegistry? registry);

    public abstract TypeRegistry Get(int id);
}
