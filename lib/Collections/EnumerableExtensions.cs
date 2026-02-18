using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ProceduralGraph.Collections
{
    internal static class EnumerableExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NET6_0_OR_GREATER
        public static bool TryGetNonEnumeratedCount<T>(this IEnumerable<T> source, out int count)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source, nameof(source));
#else
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
#endif
            return System.Linq.Enumerable.TryGetNonEnumeratedCount(source, out count);
        }
#else
        public static bool TryGetNonEnumeratedCount<T>(this IEnumerable<T> source, out int count)
        {
            switch (source)
            {
                case null:
                    throw new ArgumentNullException(nameof(source));
                case ICollection<T> collection:
                    count = collection.Count;
                    return true;
                case IReadOnlyCollection<T> readOnlyCollection:
                    count = readOnlyCollection.Count;
                    return true;
                default:
                    count = 0;
                    return false;
            }
        }
#endif
        }
    }
