using BenchmarkDotNet.Attributes;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Collections.Benchmarks;

[MemoryDiagnoser]
public class TypeLookupBenchmarks
{
    [Params(10, 100, 1000)]
    public int TypeCount { get; set; }

    [Params(false, true)]
    public bool Polymorphic { get; set; }

    private Func<object>? _getOneDelegate;
    private Func<int>? _countDelegate;
    private Func<bool>? _addDelegate;
    private Func<bool>? _removeDelegate;

    [GlobalSetup]
    public void Setup()
    {
        TypeLookup typeLookup = [];

        Type[] typesToInsert;
        Type targetType;

        if (Polymorphic)
        {
            Type baseType = TypeFactory.Define();
            Type[] leafTypes = TypeFactory.Define(baseType, TypeCount);
            targetType = baseType;
            typesToInsert = leafTypes;
        }
        else
        {
            typesToInsert = TypeFactory.Define(TypeCount);
            targetType = typesToInsert[TypeCount / 2];
        }

        object[] sampleInstances = new object[typesToInsert.Length];
        ref Type typeRef = ref MemoryMarshal.GetArrayDataReference(typesToInsert);
        foreach (ref object instance in sampleInstances.AsSpan())
        {
            instance = Activator.CreateInstance(typeRef)!;
            typeLookup.Add(instance, typeRef);
            typeRef = ref Unsafe.Add(ref typeRef, 1);
        }

        // There is a minor performance penalty for using the non-generic methods over the ones,
        // so we compile delegates for them to demonstrate the best-case scenario.

        _getOneDelegate = CompileGetOneDelegate(typeLookup, targetType);
        _countDelegate = CompileCountDelegate(typeLookup, targetType);

        object sample = sampleInstances[0];
        Type sampleType = sample.GetType();

        _addDelegate = CompileMutationDelegate(typeLookup, nameof(TypeLookup.Add), sampleType, sample);
        _removeDelegate = CompileMutationDelegate(typeLookup, nameof(TypeLookup.Remove), sampleType, sample);
    }

    [Benchmark]
    public void GetOne() => _getOneDelegate!();

    [Benchmark]
    public int GetAll() => _countDelegate!();

    [Benchmark]
    public void AddAndRemove()
    {
        _addDelegate!();
        _removeDelegate!();
    }

    private static int Count<T>(ReadOnlyTypeLookup.Query<T> query) where T : class
    {
        int count = 0;

        foreach (T _ in query)
        {
            count++;
        }

        return count;
    }

    private static Func<object> CompileGetOneDelegate(TypeLookup lookup, Type type)
    {
        MethodInfo getOneMethod = typeof(TypeLookup).GetMethod(nameof(TypeLookup.GetOne), genericParameterCount: 1, types: [])!;
        MethodInfo genericGetOneMethod = getOneMethod.MakeGenericMethod(type);

        var instanceParam = Expression.Constant(lookup);
        var callExpression = Expression.Call(instanceParam, genericGetOneMethod);
        var convertExpression = Expression.Convert(callExpression, typeof(object));

        return Expression.Lambda<Func<object>>(convertExpression).Compile();
    }

    private static Func<int> CompileCountDelegate(TypeLookup lookup, Type type)
    {
        MethodInfo getAllMethod = typeof(TypeLookup).GetMethod(nameof(TypeLookup.GetAll), genericParameterCount: 1, types: [])!;
        MethodInfo genericGetAllMethod = getAllMethod.MakeGenericMethod(type);

        MethodInfo countMethod = typeof(TypeLookupBenchmarks).GetMethod(nameof(Count), BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo genericCountMethod = countMethod.MakeGenericMethod(type);

        var instanceParam = Expression.Constant(lookup);
        var callExpression = Expression.Call(instanceParam, genericGetAllMethod);
        var countCallExpression = Expression.Call(genericCountMethod, callExpression);

        return Expression.Lambda<Func<int>>(countCallExpression).Compile();
    }

    private static Func<bool> CompileMutationDelegate(TypeLookup lookup, string methodName, Type type, object item)
    {
        MethodInfo genericMethod = typeof(TypeLookup)
            .GetMethods()
            .Single(m => m.Name == methodName && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
            .MakeGenericMethod(type);

        var instanceParam = Expression.Constant(lookup);
        var itemParam = Expression.Constant(item, type);
        var callExpression = Expression.Call(instanceParam, genericMethod, itemParam);

        return Expression.Lambda<Func<bool>>(callExpression).Compile();
    }
}