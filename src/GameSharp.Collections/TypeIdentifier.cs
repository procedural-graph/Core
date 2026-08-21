using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit, Size = 4)]
internal struct TypeIdentifier : IEquatable<TypeIdentifier>, IComparable<TypeIdentifier>
{
    [FieldOffset(0)]
    private byte b0;

    [FieldOffset(2)]
    private byte b2;

    public ushort TypeID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Unsafe.ReadUnaligned<ushort>(in BitConverter.IsLittleEndian ? ref b0 : ref b2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Unsafe.WriteUnaligned(ref BitConverter.IsLittleEndian ? ref b0 : ref b2, value);
    }

    public short AssemblyID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly get => Unsafe.ReadUnaligned<short>(in BitConverter.IsLittleEndian ? ref b2 : ref b0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Unsafe.WriteUnaligned(ref BitConverter.IsLittleEndian ? ref b2 : ref b0, value);
    }

    public readonly int CompositeKey
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.BitCast<TypeIdentifier, int>(this);
    }

    public readonly bool Equals(TypeIdentifier other) => CompositeKey == other.CompositeKey;

    public readonly int CompareTo(TypeIdentifier other)
    {
        return CompositeKey.CompareTo(other.CompositeKey);
    }

    public readonly override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is TypeIdentifier other && Equals(other);
    }

    public readonly override int GetHashCode()
    {
        return CompositeKey.GetHashCode();
    }

    public readonly override string ToString()
    {
        return CompositeKey.ToString("X8");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator TypeIdentifier(int value) => Unsafe.BitCast<int, TypeIdentifier>(value);
}