using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections;

/// <summary>
/// Provides a base registry for managing type information and inheritance relationships.
/// </summary>
/// <remarks>
/// Does not support <see cref="System.Runtime.Loader.AssemblyLoadContext"/> unloading.
/// </remarks>
public abstract class TypeRegistry
{
    private abstract class TypeInfo : ITypeInfo
    {
        public abstract Type Type { get; }

        public abstract int ID { get; }

        private ImmutableArray<int> _derivedTypeIDs;
        public ImmutableArray<int> DerivedTypeIDs
        {
            get => _derivedTypeIDs;
            protected init => _derivedTypeIDs = value;
        }

        public bool AddRelationTo(int typeID)
        {
            return ImmutableInterlocked.Update(ref _derivedTypeIDs, InsertSorted, typeID);
        }

        private ImmutableArray<int> InsertSorted(ImmutableArray<int> ids, int id)
        {
            if (TryGetIndexOf(ids.AsSpan(), id, out int index))
            {
                return ids;
            }

            return ids.Insert(index, id);
        }
    }

    private sealed class DynamicTypeInfo : TypeInfo
    {
        public override Type Type { get; }

        public override int ID { get; }

        public DynamicTypeInfo(Type type, int id, int inheritorID)
        {
            Type = type;
            ID = id;
            int[] ids = [id, inheritorID];
            ids.Sort();
            DerivedTypeIDs = ImmutableCollectionsMarshal.AsImmutableArray(ids);
        }
    }

    private sealed class StaticTypeInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T> : TypeInfo
    {
        public static TypeInfo Default { get; }

        public override Type Type => typeof(T);

        public override int ID { get; }

        static StaticTypeInfo()
        {
            _syncRoot.EnterUpgradeableReadLock();
            
            try
            {
                if (!_registrations.TryGetValue(typeof(T), out TypeInfo? typeInfo))
                {
                    _syncRoot.EnterWriteLock();

                    try
                    {
                        typeInfo = new StaticTypeInfo<T>(_registrations.Count);
                        _registrations.Add(typeof(T), typeInfo);
                    }
                    finally
                    {
                        _syncRoot.ExitWriteLock();
                    }
                }

                for (Type? type = typeof(T).BaseType; type is { }; type = type.BaseType)
                {
                    InheritFrom(type, typeInfo.ID);
                }

                foreach (Type type in typeof(T).GetInterfaces())
                {
                    InheritFrom(type, typeInfo.ID);
                }

                // If type 'T' was already registered implicitly because 
                // a derived type called InheritFrom(typeof(T)), 'typeInfo' will actually be 
                // a DynamicTypeInfo instance, NOT a StaticTypeInfo instance. 
                // This is intentional. By assigning that existing DynamicTypeInfo to Default, 
                // we guarantee reference equality for ITypeInfo instances across the registry 
                // and prevent static initialization deadlocks.

                Default = typeInfo;
            }
            finally
            {
                _syncRoot.ExitUpgradeableReadLock();
            }
        }

        private StaticTypeInfo(int id)
        {
            ID = id;
            DerivedTypeIDs = [id];
        }

        private static void InheritFrom(Type type, int id)
        {
            if (_registrations.TryGetValue(type, out TypeInfo? typeInfo))
            {
                typeInfo.AddRelationTo(id);
                return;
            }

            _syncRoot.EnterWriteLock();

            try
            {
                typeInfo = new DynamicTypeInfo(type, _registrations.Count, id);
                _registrations.Add(type, typeInfo);
            }
            finally
            {
                _syncRoot.ExitWriteLock();
            }
        }
    }

    private static readonly OrderedDictionary<Type, TypeInfo> _registrations = [];
    private static readonly ReaderWriterLockSlim _syncRoot = new(LockRecursionPolicy.NoRecursion);

    /// <summary>
    /// Retrieves the type information associated with the specified unique ID.
    /// </summary>
    /// <param name="id">The unique ID of the type.</param>
    /// <returns>The type information associated with <paramref name="id"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if no type registration was found for the specified ID.</exception>
    [MethodImpl(MethodImplOptions.NoInlining)]
    protected static ITypeInfo GetTypeInfo(int id)
    {
        _syncRoot.EnterReadLock();

        try
        {
            return _registrations.GetAt(id).Value;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ArgumentException("A type registration with the specified ID was not found.", ex);
        }
        finally
        {
            _syncRoot.ExitReadLock();
        }
    }

    /// <summary>
    /// Retrieves the type information for the specified type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to retrieve information for.</typeparam>
    /// <returns>The type information for <typeparamref name="T"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ITypeInfo GetTypeInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>()
    {
        return StaticTypeInfo<T>.Default;
    }

    /// <summary>
    /// Attempts to retrieve the type information for a specified <see cref="Type"/>.
    /// </summary>
    /// <remarks>
    /// If just-in-time (JIT) compilation is available, a type registration will be automatically created for 
    /// <paramref name="type"/> if one does not already exist.
    /// </remarks>
    /// <param name="type">The <see cref="Type"/> to look up.</param>
    /// <param name="typeInfo">
    /// When this method returns, contains the type information for <paramref name="type"/> or <see langword="null"/> if
    /// no type registration was created.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a type registration was created or found for <paramref name="type"/>; 
    /// otherwise <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool TryGetTypeInfo(Type type, [NotNullWhen(true)] out ITypeInfo? typeInfo)
    {
        if (TryGetExistingTypeInfo(type, out typeInfo))
        {
            return true;
        }

        if (RuntimeFeature.IsDynamicCodeSupported)
        {
            typeInfo = GetTypeInfo(type);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Retrieves the type information for the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to retrieve information for.</param>
    /// <returns>The <see cref="ITypeInfo"/> associated with the provided <paramref name="type"/>.</returns>
    [RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])"), SuppressMessage("Trimming", "IL2071"), MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static ITypeInfo GetTypeInfo(Type type)
    {
        Type infoType = typeof(StaticTypeInfo<>).MakeGenericType(type)!;
        PropertyInfo property = infoType.GetProperty(nameof(StaticTypeInfo<>.Default), BindingFlags.Public | BindingFlags.Static)!;
        return Unsafe.As<ITypeInfo>(property.GetValue(null)!);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool TryGetExistingTypeInfo(Type type, [NotNullWhen(true)] out ITypeInfo? typeInfo)
    {
        _syncRoot.EnterReadLock();

        try
        {
            if (_registrations.TryGetValue(type, out TypeInfo? typedTypeInfo))
            {
                typeInfo = typedTypeInfo;
                return true;
            }

            typeInfo = null;
            return false;
        }
        finally
        {
            _syncRoot.ExitReadLock();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected static bool TryGetIndexOf(ReadOnlySpan<int> ids, int id, out int index)
    {
        ids.HybridSearch(id, out int byteOffset, out bool exists);
        index = byteOffset >> 2;
        return exists;
    }
}
