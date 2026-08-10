using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ProceduralGraph;

internal static class ConcurrentDictionaryExtensions
{
    public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, KeyValuePair<TKey, TValue> item) where TKey : notnull
    {
        return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
    }
}