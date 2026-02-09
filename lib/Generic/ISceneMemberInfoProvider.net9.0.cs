// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
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