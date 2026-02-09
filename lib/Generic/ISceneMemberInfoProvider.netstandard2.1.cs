// Copyright (c) 2026 William Brocklesby. All rights reserved.
// 
// This source code is the proprietary property of William Brocklesby.
// Use, distribution, or modification of this file is strictly prohibited 
// without express written permission.
//
// Internal Use Only.
using System;
using System.Collections.Generic;

namespace ProceduralGraph.Generic
{
    public partial interface ISceneMemberInfoProvider<TKey, TValue> where TKey : struct, IEquatable<TKey> where TValue : class
    {
        /// <summary>
        /// Determines whether the specified <typeparamref name="TKey"/> equals the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The <typeparamref name="TKey"/> instance to compare.</param>
        /// <param name="value">The <typeparamref name="TValue"/> instance to compare.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <typeparamref name="TKey"/> equals the specified <paramref name="value"/>; 
        /// otherwise, <see langword="false"/>.
        /// </returns>
        bool Equals(TKey key, TValue value);

        /// <summary>
        /// Returns a hash code for the specified <typeparamref name="TKey"/> instance.
        /// </summary>
        /// <param name="key">The <typeparamref name="TKey"/> instance for which a hash code is to be returned.</param>
        /// <returns>A hash code for the specified <typeparamref name="TKey"/> instance.</returns>
        int GetHashCode(TKey key);
    }
}