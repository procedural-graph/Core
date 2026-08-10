using GameSharp.ProceduralGraph.Mathematics;
using ProceduralGraph;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace GameSharp.ProceduralGraph.Collections.Unsafe;

using _Unsafe = System.Runtime.CompilerServices.Unsafe;

/// <summary>
/// Provides a two-dimensional, fixed-size, contiguous block of unmanaged memory for elements of type <typeparamref name="TValue"/>, 
/// supporting collection-like access and manipulation.
/// </summary>
/// <typeparam name="TValue">The unmanaged value type of elements stored in the memory region.</typeparam>
public abstract partial class UnmanagedMap<TValue> : UnmanagedMemory<TValue>, IBigCollection<TValue> where TValue : unmanaged
{
    /// <summary>
    /// Provides a forward-only, read-only cursor for sequentially reading rows of unmanaged memory containing values of
    /// type <typeparamref name="TValue"/>.
    /// </summary>
    public ref struct RowReader : IDisposable
    {
        private static readonly unsafe uint _sizeInBytes = AsUInt32Unsafe(sizeof(TValue));
        private readonly SafeHandle _handle;
        private unsafe TValue* _pos;
        private readonly unsafe TValue* _end;
        private uint _remaining;
        private bool _initialized;

        /// <inheritdoc cref="IEnumerator.Current"/>
        public unsafe TValue Current => *_pos;

        internal unsafe RowReader(UnmanagedMap<TValue> map, long offset)
        {
            _handle = map.Handle;
            _pos = ((TValue*)_handle.AddRef()) + offset;
            _end = _pos + map.Size.X;
            _remaining = Saturate((ulong)(_end - _pos));
        }

        /// <summary>
        /// Reads a sequence of values from the current row into the specified <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">The span to write the values into.</param>
        /// <inheritdoc cref="Read(ref TValue, uint)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(Span<TValue> buffer)
        {
            return Read(ref buffer[0], AsUInt32Unsafe(buffer.Length));
        }

        /// <inheritdoc cref="Read(ref TValue, uint)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(ref TValue destination, int length)
        {
            return length > 0 ? Read(ref destination, AsUInt32Unsafe(length)) : 0;
        }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector"></param>
        /// <inheritdoc cref="Read(ref TValue, uint)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(ref System.Numerics.Vector<TValue> vector)
        {
            ref TValue component = ref _Unsafe.As<System.Numerics.Vector<TValue>, TValue>(ref vector);
            return Read(ref component, AsUInt32Unsafe(System.Numerics.Vector<TValue>.Count));
        }
#endif

        /// <summary>
        /// Reads a specified number of values from the current row into a destination starting at the provided reference.
        /// </summary>
        /// <param name="destination">A reference to the first element of the destination where values should be written.</param>
        /// <param name="length">The number of values to read.</param>
        /// <returns>The total number of values successfully read.</returns>
        public unsafe int Read(ref TValue destination, uint length)
        {
            uint count = Math.Min(length, _remaining), bytesToCopy;
            ref byte destBytes = ref _Unsafe.As<TValue, byte>(ref destination);
            ref byte srcBytes = ref *(byte*)_pos;
            ulong totalBytes = count * _sizeInBytes;
        Read:
            bytesToCopy = Saturate(totalBytes);
            _Unsafe.CopyBlockUnaligned(ref destBytes, ref srcBytes, bytesToCopy);
            totalBytes -= bytesToCopy;
            if (totalBytes <= 0L)
            {
                Advance(count);
                return (int)count;
            }
            _Unsafe.AddByteOffset(ref destBytes, bytesToCopy);
            _Unsafe.AddByteOffset(ref srcBytes, bytesToCopy);
            goto Read;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext()"/>
        public unsafe bool MoveNext()
        {
            if (!_initialized)
            {
                _initialized = true;
                return true;
            }

            TValue* newPos = _pos + 1;
            if (newPos > _end)
            {
                return false;
            }
            _pos = newPos;
            _remaining = Saturate((ulong)(_end - _pos));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void Advance(uint count)
        {
            _initialized = true;
            _pos += count;
            _remaining = Saturate((ulong)(_end - _pos));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Saturate(ulong value)
        {
            const ulong UIntMaxValueAsULong = uint.MaxValue;
            return value > UIntMaxValueAsULong ? (uint)value : uint.MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint AsUInt32Unsafe(int value)
        {
#if NET8_0_OR_GREATER
            return _Unsafe.BitCast<int, uint>(value);
#else
            return (uint)value;
#endif
        }

        /// <inheritdoc/>
        public unsafe void Dispose()
        {
            if (_pos != null)
            {
                _handle.DangerousRelease();
                _pos = null;
            }
        }
    }

    /// <summary>
    /// Represents a read-only view of a single row within an unmanaged, two-dimensional map structure.
    /// </summary>
    public readonly struct Row : IReadOnlyCollection<TValue>
    {
        private sealed class Enumerator : IEnumerator<TValue>
        {
            private readonly SafeHandle _handle;
            private unsafe TValue* _pos;
            private unsafe readonly TValue* _end;

            public unsafe Enumerator(UnmanagedMap<TValue> map, long offset)
            {
                _handle = map.Handle;
                _pos = ((TValue*)_handle.AddRef()) + offset - 1;
                _end = _pos + map.Size.X;
            }

            public unsafe TValue Current => *_pos;

            object IEnumerator.Current => Current;

            public unsafe void Dispose()
            {
                if (_pos != null)
                {
                    _handle.DangerousRelease();
                    _pos = null;
                }
            }

            public unsafe bool MoveNext()
            {
                return ++_pos < _end;
            }

            void IEnumerator.Reset()
            {
                throw new NotSupportedException();
            }
        }

        private readonly UnmanagedMap<TValue> _map;
        private readonly long _offset;

        internal Row(UnmanagedMap<TValue> map, long offset)
        {
            _offset = offset;
            _map = map;
        }

        /// <inheritdoc/>
        public long Count => _map.Size.X;
        int IReadOnlyCollection<TValue>.Count => checked((int)Count);

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator()"/>
        public RowReader GetEnumerator() => new(_map, _offset);

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => new Enumerator(_map, _offset);

        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_map, _offset);
    }

    /// <summary>
    /// Gets the dimensions of the two-dimensional memory block.
    /// </summary>
    public abstract Long2 Size { get; }

    /// <summary>
    /// Gets or sets the element at the specified two-dimensional coordinates within the buffer.
    /// </summary>
    public unsafe TValue this[long x, long y]
    {
        get
        {
            long position = PositionFromIndices(x, y);
            IntPtr ptr = Handle.AddRef();
            TValue value = ((TValue*)ptr)[position];
            Handle.DangerousRelease();
            return value;
        }
        set
        {
            long position = PositionFromIndices(x, y);
            IntPtr ptr = Handle.AddRef();
            ((TValue*)ptr)[position] = value;
            Handle.DangerousRelease();
        }
    }

    /// <summary>
    /// Gets the row at the specified vertical index.
    /// </summary>
    /// <param name="y">The zero-based vertical index of the row to retrieve.</param>
    /// <returns>A <see cref="Row"/> representing the row at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="y"/> is greater than or equal to <c><see cref="Size"/>.Y</c></exception>
    public Row this[long y]
    {
        get
        {
            long position = PositionFromIndices(0L, y);
            return new Row(this, position);
        }
    }

    /// <inheritdoc/>
    public override bool Contains(TValue item) => Handle.Contains(item, (ulong)Length);

    /// <summary>
    /// Creates a new unmanaged map with the rows and columns transposed from the current map.
    /// </summary>
    /// <returns>An unmanaged map source containing the transposed data. The returned map has its dimensions swapped such that
    /// rows become columns and columns become rows.</returns>
    public unsafe UnmanagedMapSource<TValue> Transpose()
    {
        const int blockSize = 16;
        IntPtr srcPtr = Handle.AddRef(), dstPtr = IntPtr.Zero;
        UnmanagedMapSource <TValue> transposed = new(Size.Y, Size.X);
        try
        {
            dstPtr = transposed.Handle.AddRef();
            long totalRowBlocks = (Size.Y + blockSize - 1) / blockSize;
            Parallel.For(0, totalRowBlocks, rowBlockIndex =>
            {
                long rStart = rowBlockIndex * blockSize;
                long rEnd = Math.Min(rStart + blockSize, Size.Y);

                for (long cStart = 0; cStart < Size.X; cStart += blockSize)
                {
                    long cEnd = Math.Min(cStart + blockSize, Size.X);
                    for (long r = rStart; r < rEnd; r++)
                    {
                        long sourceRowOffset = r * Size.X;
                        for (long c = cStart; c < cEnd; c++)
                        {
                            ((TValue*)dstPtr)[c * Size.Y + r] = ((TValue*)srcPtr)[sourceRowOffset + c];
                        }
                    }
                }
            });

            return transposed;
        }
        catch
        {
            transposed.Dispose();
            throw;
        }
        finally
        {
            if (dstPtr != IntPtr.Zero)
            {
                transposed.Handle.DangerousRelease();
            }

            Handle.DangerousRelease();
        }
    }

    

    [Guard(DisposalState = nameof(Disposed))]
    private partial long PositionFromIndices([Index(Length = nameof(Size.X))] long x, [Index(Length = nameof(Size.Y))] long y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long PositionFromIndicesImpl(long x, long y)
    {
        return y * Size.X + x;
    }
}