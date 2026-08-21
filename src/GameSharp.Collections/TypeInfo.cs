using GameSharp.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GameSharp.Collections;

/// <summary>
/// Represents type information for a registered type in a <see cref="TypeRegistry"/>.
/// </summary>
public sealed class TypeInfo
{
    private static readonly TypeRegistryProvider _registryProvider;

    /// <summary>
    /// Gets the <see cref="System.Type"/> represented by this type info.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the unique ID of this type.
    /// </summary>
    public int ID { get; }

    /// <summary>
    /// Gets a list of the types derived from this type.
    /// </summary>
    public DerivedTypeCollection Derived { get; }

    static TypeInfo()
    {
        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            _registryProvider = new Collectible.TypeRegistryProvider();
        }
        else
        {
            _registryProvider = new Static.TypeRegistryProvider();
        }
    }

    internal TypeInfo(Type type, int id)
    {
        Type = type;
        ID = id;
        Derived = new DerivedTypeCollection(ID);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TypeInfo other && ID == other.ID;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }

    /// <summary>
    /// Gets the <see cref="TypeInfo"/> for the specified type ID from the <see cref="TypeRegistry"/>
    /// </summary>
    /// <param name="id">The ID of the type for which to get the <see cref="TypeInfo"/>.</param>
    /// <returns>The <see cref="TypeInfo"/> for the specified type ID.</returns>
    public static TypeInfo Get(int id)
    {
        TypeIdentifier identifier = (TypeIdentifier)id;
        TypeRegistry registry = _registryProvider.Get(identifier.AssemblyID);
        return registry.Get(identifier.TypeID);
    }

    /// <summary>
    /// Gets the <see cref="TypeInfo"/> for the specified type <typeparamref name="T"/> from the <see cref="TypeRegistry"/> 
    /// associated with the assembly of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type for which to get the <see cref="TypeInfo"/>.</typeparam>
    /// <returns>The <see cref="TypeInfo"/> for the specified type <typeparamref name="T"/>.</returns>
    public static TypeInfo Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
    {
        if (RuntimeFeature.IsDynamicCodeSupported && typeof(T).Assembly.IsCollectible)
        {
            return Get(typeof(T));
        }

        return TypeInfo<T>.Default;
    }

    /// <summary>
    /// Gets the <see cref="TypeInfo"/> for the specified <paramref name="type"/> from the <see cref="TypeRegistry"/>
    /// associated with the assembly of the specified <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The type for which to get the <see cref="TypeInfo"/>.</param>
    /// <returns>The <see cref="TypeInfo"/> for the specified <paramref name="type"/>.</returns>
    public static TypeInfo Get([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        TypeRegistry registry = _registryProvider.GetOrAdd(type);

        if (registry.GetOrAdd(type, out TypeInfo? typeInfo))
        {
            return typeInfo;
        }

        for (Type? baseType = type.BaseType; baseType is { }; baseType = baseType.BaseType)
        {
            typeInfo.AddDerived(baseType);
        }

        foreach (Type iface in type.GetInterfaces())
        {
            typeInfo.AddDerived(iface);
        }

        return typeInfo;
    }

    /// <summary>
    /// Attempts to get the <see cref="TypeInfo"/> for the specified <paramref name="type"/> from the <see cref="TypeRegistry"/>
    /// </summary>
    /// <param name="type">The type for which to get the <see cref="TypeInfo"/>.</param>
    /// <param name="typeInfo">
    /// When this method returns, contains the <see cref="TypeInfo"/> associated with the specified <paramref name="type"/>, 
    /// if the type is found; otherwise, <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> if the <see cref="TypeInfo"/> was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGet([NotNullWhen(true)] Type? type, [NotNullWhen(true)] out TypeInfo? typeInfo)
    {
        if (type is { } && _registryProvider.TryGet(type, out TypeRegistry? registry))
        {
            return registry.TryGet(type, out typeInfo);
        }

        typeInfo = null;
        return false;
    }

    private void AddDerived(Type type)
    {
        TypeRegistry registry = _registryProvider.GetOrAdd(type);
        registry.GetOrAdd(type, out TypeInfo typeInfo);
        typeInfo.Derived.Add(ID);
    }
}
