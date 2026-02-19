using System;
using System.Collections.Generic;

namespace ProceduralGraph.Generic;

public partial interface ISceneMemberInfoProvider<TKey, TValue> : IAlternateEqualityComparer<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : class
{
    TValue IAlternateEqualityComparer<TKey, TValue>.Create(TKey alternate)
    {
        if (TryFind(alternate, out var value))
        {
            return value;
        }

        throw new KeyNotFoundException($"The alternate key {alternate} was not found.");
    }
}