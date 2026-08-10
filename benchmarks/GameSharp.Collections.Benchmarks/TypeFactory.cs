using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections.Benchmarks;

internal static class TypeFactory
{
    private static ModuleBuilder ModuleBuilder { get; }

    static TypeFactory()
    {
        AssemblyName assemblyName = new($"DynamicTypes_{Guid.NewGuid():N}");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
    }

    public static Type[] Define(int count)
    {
        Type[] types = new Type[count];

        foreach (ref Type type in types.AsSpan())
        {
            type = Define();
        }

        return types;
    }

    public static Type[] Define(Type baseType, int depth)
    {
        Type[] leafTypes = new Type[depth];

        foreach (ref Type leafType in leafTypes.AsSpan())
        {
            leafType = Define(baseType);
            baseType = leafType;
        }

        return leafTypes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type Define(Type? parent = null)
    {
        TypeBuilder builder = ModuleBuilder.DefineType($"T_{Guid.NewGuid():N}", TypeAttributes.Public | TypeAttributes.Class, parent);
        return builder.CreateType();
    }
}
