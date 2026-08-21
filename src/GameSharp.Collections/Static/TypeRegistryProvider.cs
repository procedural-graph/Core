using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Static;

internal class TypeRegistryProvider : Collections.TypeRegistryProvider
{
    private static readonly TypeRegistry _instance = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override TypeRegistry Get(int id) => _instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override TypeRegistry GetOrAdd(Type type) => _instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool TryGet(Type type, [NotNullWhen(true)] out Collections.TypeRegistry? registry)
    {
        registry = _instance;
        return true;
    }
}
