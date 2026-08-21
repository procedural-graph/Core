using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

internal abstract class TypeRegistry
{
    protected OrderedDictionary<Type, TypeInfo> Registrations { get; } = [];

    protected virtual int NextID => Registrations.Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual TypeInfo Get(int id)
    {
        return Registrations.GetAt(id).Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool TryGet(Type type, [MaybeNullWhen(false)] out TypeInfo typeInfo)
    {
        return Registrations.TryGetValue(type, out typeInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool GetOrAdd(Type type, [NotNull] out TypeInfo? typeInfo)
    {
        if (Registrations.TryGetValue(type, out typeInfo))
        {
            return true;
        }

        typeInfo = new TypeInfo(type, NextID);
        Registrations.Add(type, typeInfo);

        return false;
    }
}
