using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace GameSharp.Collections.Tests.TypeLookup;

internal static class Extensions
{
    extension(Assert)
    {
        [StackTraceHidden]
        public static void Single<TLookup, TItem>(TLookup result, TItem item)
            where TLookup : ReadOnlyTypeLookup
            where TItem : class
        {
            Assert.Single(result);
            Assert.True(result.Contains(item));
            Assert.True(result.Contains(item, typeof(TItem)));
        }

        [StackTraceHidden]
        public static void Single<TItem>(ReadOnlyTypeLookup.Query<TItem> query, TItem expected) where TItem : class
        {
            TItem[] items = [.. query];
            Assert.Single(items, expected);
        }

        [StackTraceHidden]
        public static void Single(ReadOnlyTypeLookup.Query query, object expected)
        {
            object[] items = [.. query];
            Assert.Single(items, expected);
        }

        [StackTraceHidden]
        public static void GetOneThrowsForMissingItems<TLookup, TItem>(TLookup lookup)
            where TLookup : ReadOnlyTypeLookup
            where TItem : class
        {
            Assert.Throws<ArgumentException>(lookup.GetOne<TItem>);
            Assert.Throws<ArgumentException>(() => lookup.GetOne(typeof(TItem)));

            Assert.Throws<ArgumentException>(lookup.GetOne<TItem>);
            Assert.Throws<ArgumentException>(() => lookup.GetOne(typeof(TItem)));
        }

        [StackTraceHidden]
        public static void TryGetOneReturnsFalseForMissingItems<TLookup, TItem>(TLookup lookup)
            where TLookup : ReadOnlyTypeLookup
            where TItem : class
        {
            Assert.False(lookup.TryGetOne(out TItem? _));
            Assert.False(lookup.TryGetOne(typeof(TItem), out object? _));

            Assert.False(lookup.TryGetOne(out TItem? _));
            Assert.False(lookup.TryGetOne(typeof(TItem), out object? _));
        }

        [StackTraceHidden]
        public static void SequenceEqualsUnordered<TItem>(HashSet<object> expected, ReadOnlyTypeLookup.Query<TItem> actual) where TItem : class
        {
            foreach (object item in actual)
            {
                Assert.Removed(expected, item);
            }

            Assert.NoneRemaining(expected);
        }

        [StackTraceHidden]
        public static void SequenceEqualsUnordered(HashSet<object> expected, ReadOnlyTypeLookup.Query actual)
        {
            foreach (object item in actual)
            {
                Assert.Removed(expected, item);
            }

            Assert.NoneRemaining(expected);
        }

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Removed<T>(HashSet<T> set, T item) where T : notnull
        {
            Assert.True(set.Remove(item), $"Unexpected '{item}' was returned.");
        }

        [StackTraceHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NoneRemaining<T>(HashSet<T> set) where T : notnull
        {
            Assert.True(set.Count == 0, $"Not all expected items were returned. Missing: {string.Join(", ", set)}");
        }
    }
}
