using System;
using System.Runtime.InteropServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Numerics;
using System.Runtime.CompilerServices;
#endif

namespace ProceduralGraph;

[StructLayout(LayoutKind.Sequential)]
internal readonly record struct IntegerLookup(int Key, int Index) : IComparable, IComparable<int>, IComparable<IntegerLookup>
{
#if NETCOREAPP3_0_OR_GREATER
    private static readonly Vector<int> _altMask;
    private static readonly int _indicesPerVector = Vector<int>.Count / 2;

    static IntegerLookup()
    {
        if (!Vector.IsHardwareAccelerated)
        {
            return;
        }

        Span<int> mask = stackalloc int[Vector<int>.Count];
        for (int i = 0; i < mask.Length; i++)
        {
            mask[i] = i % 2;
        }

        _altMask = new Vector<int>(mask);
    }
#endif

    public int CompareTo(object? obj) => obj is IntegerLookup other ? CompareTo(other) : 1;

    public int CompareTo(int other) => Key.CompareTo(other);

    public int CompareTo(IntegerLookup other) => Key.CompareTo(other.Key);

    public static void Offset(Span<IntegerLookup> indices, int count)
    {
        int index = 0, length = indices.Length;

#if NETCOREAPP3_0_OR_GREATER
        if (Vector.IsHardwareAccelerated)
        {
            Vector<int> offset = _altMask * count;
            for (; (length - index) >= _indicesPerVector; index += _indicesPerVector)
            {
                Unsafe.As<IntegerLookup, Vector<int>>(ref indices[index]) += offset;
            }
        }
#endif

        for (; index < length; index++)
        {
            ref IntegerLookup newEntry = ref indices[index];
            newEntry = newEntry with { Index = newEntry.Index + count };
        }
    }
}