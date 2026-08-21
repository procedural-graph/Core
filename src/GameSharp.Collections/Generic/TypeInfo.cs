using System.Diagnostics.CodeAnalysis;

namespace GameSharp.Collections.Generic;

internal static class TypeInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>
{
    public static TypeInfo Default { get; } = TypeInfo.Get(typeof(T));
}
