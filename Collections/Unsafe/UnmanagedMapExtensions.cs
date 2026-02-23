using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ProceduralGraph.Collections.Unsafe
{
    /// <summary>
    /// Provides extension methods for processing points on an unmanaged map, enabling efficient parallel operations on
    /// map data. 
    /// </summary>
    public static class UnmanagedMapExtensions
    {
        /// <summary>
        /// Executes a specified operation on each element of the map in parallel. 
        /// </summary>
        /// <typeparam name="TSource">The unmanaged type of the elements in the source map.</typeparam>
        /// <typeparam name="TOperation">The type of the operation to apply, which must implement <see cref="IMapOperation{TSource, TSource}"/>.</typeparam>
        /// <param name="source">The <see cref="UnmanagedMap{TSource}"/> to iterate over. </param>
        /// <param name="operation">The operation to apply to each element.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>. </exception>
        /// <exception cref="ObjectDisposedException">Thrown when the <paramref name="source"/> map has been disposed.</exception>
        public static unsafe void ForEach<TSource, TOperation>(this UnmanagedMap<TSource> source, TOperation operation)
            where TSource : unmanaged
            where TOperation : struct, IMapOperation<TSource, TSource>
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ObjectDisposedException.ThrowIf(source.disposed, source);
#else
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.disposed)
            {
                throw new ObjectDisposedException(source.GetType().FullName);
            }
#endif
            int width = source.Width;
            int height = source.Height;
            TSource* baseBuffer = source.buffer;
            Parallel.For(0, height, y =>
            {
                TSource* rowOffset = baseBuffer + (y * width);
                for (int x = 0; x < width; x++)
                {
                    ref TSource valueRef = ref *(rowOffset + x);
                    valueRef = operation.Apply(x, y, valueRef);
                }
            });
        }

        /// <summary>
        /// Executes a SIMD-optimized operation on each element of the map in parallel. 
        /// </summary>
        /// <typeparam name="TSource">The unmanaged type of the elements in the source map.</typeparam>
        /// <typeparam name="TOperation">The type of the SIMD operation to apply. </typeparam>
        /// <param name="source">The <see cref="UnmanagedMap{TSource}"/> to iterate over.</param>
        /// <param name="operation">The SIMD-capable operation to apply.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the <paramref name="source"/> map has been disposed.</exception>
        public static unsafe void FastForEach<TSource, TOperation>(this UnmanagedMap<TSource> source, TOperation operation)
            where TSource : unmanaged
            where TOperation : struct, ISimdMapOperation<TSource, TSource>
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ObjectDisposedException.ThrowIf(source.disposed, source);
#else
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.disposed)
            {
                throw new ObjectDisposedException(source.GetType().FullName);
            }
#endif
            int width = source.Width;
            int height = source.Height;
            TSource* baseBuffer = source.buffer;
            int vectorSize = Vector<TSource>.Count;
            Parallel.For(0, height, y =>
            {
                TSource* rowOffset = baseBuffer + (y * width);
                int x = 0;

                for (; x <= width - vectorSize; x += vectorSize)
                {
                    TSource* ptr = rowOffset + x;
                    Vector<TSource> dataChunk = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Vector<TSource>>(ptr);
                    Vector<TSource> result = operation.Apply(x, y, in dataChunk);
                    System.Runtime.CompilerServices.Unsafe.WriteUnaligned(ptr, result);
                }

                for (; x < width; x++)
                {
                    ref TSource valueRef = ref *(rowOffset + x);
                    valueRef = operation.Apply(x, y, valueRef);
                }
            });
        }

        /// <summary>
        /// Executes an operation that maps elements from a source map to a destination map in parallel. 
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source map.</typeparam>
        /// <typeparam name="TResult">The type of elements in the destination map.</typeparam>
        /// <typeparam name="TOperation">The type of the mapping operation. </typeparam>
        /// <param name="source">The source <see cref="UnmanagedMap{TSource}"/>.</param>
        /// <param name="destination">The destination <see cref="UnmanagedMap{TResult}"/>.</param>
        /// <param name="operation">The operation to apply to each source element to produce a destination element.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when either map has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the dimensions of the source and destination maps do not match.</exception>
        public static unsafe void ForEach<TSource, TResult, TOperation>(
            this UnmanagedMap<TSource> source,
            UnmanagedMap<TResult> destination,
            TOperation operation)
            where TSource : unmanaged
            where TResult : unmanaged
            where TOperation : struct, IMapOperation<TSource, TResult>
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ObjectDisposedException.ThrowIf(source.disposed, source);

            ArgumentNullException.ThrowIfNull(destination, nameof(destination));
            ObjectDisposedException.ThrowIf(destination.disposed, destination);

            ArgumentOutOfRangeException.ThrowIfNotEqual(source.Width, destination.Width, nameof(destination));
            ArgumentOutOfRangeException.ThrowIfNotEqual(source.Height, destination.Height, nameof(destination));
#else
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.disposed)
            {
                throw new ObjectDisposedException(source.GetType().FullName);
            }

            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (destination.disposed)
            {
                throw new ObjectDisposedException(destination.GetType().FullName);
            }

            if (source.Width != destination.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected width {source.Width}, but got {destination.Width}.");
            }
            if (source.Height != destination.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected height {source.Height}, but got {destination.Height}.");
            }
#endif
            int width = source.Width;
            int height = source.Height;
            TSource* sourceBuffer = source.buffer;
            TResult* destinationBuffer = destination.buffer;
            Parallel.For(0, height, y =>
            {
                TSource* sourceRowOffset = sourceBuffer + (y * width);
                TResult* destinationRowOffset = destinationBuffer + (y * width);
                for (int x = 0; x < width; x++)
                {
                    *(destinationRowOffset + x) = operation.Apply(x, y, in *(sourceRowOffset + x));
                }
            });
        }

        /// <summary>
        /// Performs a high-performance SIMD mapping operation from a source map to a destination map in parallel. 
        /// </summary>
        /// <typeparam name="TSource">The type of elements in the source map.</typeparam>
        /// <typeparam name="TResult">The type of elements in the destination map.</typeparam>
        /// <typeparam name="TOperation">The type of the SIMD mapping operation. </typeparam>
        /// <param name="source">The source <see cref="UnmanagedMap{TSource}"/>.</param>
        /// <param name="destination">The destination <see cref="UnmanagedMap{TResult}"/>.</param>
        /// <param name="operation">The SIMD operation to apply.</param>
        /// <exception cref="ArgumentException">Thrown when the SIMD vector sizes for <typeparamref name="TSource"/> and <typeparamref name="TResult"/> are not equal.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the dimensions of the source and destination maps do not match.</exception>
        public static unsafe void FastForEach<TSource, TResult, TOperation>(
            this UnmanagedMap<TSource> source,
            UnmanagedMap<TResult> destination,
            TOperation operation)
            where TSource : unmanaged
            where TResult : unmanaged
            where TOperation : struct, ISimdMapOperation<TSource, TResult>
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            ObjectDisposedException.ThrowIf(source.disposed, source);

            ArgumentNullException.ThrowIfNull(destination, nameof(destination));
            ObjectDisposedException.ThrowIf(destination.disposed, destination);

            ArgumentOutOfRangeException.ThrowIfNotEqual(source.Width, destination.Width, nameof(destination));
            ArgumentOutOfRangeException.ThrowIfNotEqual(source.Height, destination.Height, nameof(destination));
#else
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (source.disposed)
            {
                throw new ObjectDisposedException(source.GetType().FullName);
            }

            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (destination.disposed)
            {
                throw new ObjectDisposedException(destination.GetType().FullName);
            }

            if (source.Width != destination.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected width {source.Width}, but got {destination.Width}.");
            }
            if (source.Height != destination.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected height {source.Height}, but got {destination.Height}.");
            }
#endif
            int width = source.Width;
            int height = source.Height;
            TSource* sourceBuffer = source.buffer;
            TResult* destinationBuffer = destination.buffer;
            int vectorSize = Vector<TSource>.Count;
            if (vectorSize != Vector<TResult>.Count)
            {
                throw new ArgumentException($"Vector size mismatch: {typeof(TSource)} has vector size {vectorSize}, while {typeof(TResult)} has vector size {Vector<TResult>.Count}.", nameof(operation));
            }
            Parallel.For(0, height, y =>
            {
                TSource* sourceRowOffset = sourceBuffer + (y * width);
                TResult* destinationRowOffset = destinationBuffer + (y * width);
                int x = 0;

                for (; x <= width - vectorSize; x += vectorSize)
                {
                    TSource* sourcePtr = sourceRowOffset + x;
                    Vector<TSource> dataChunk = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Vector<TSource>>(sourcePtr);
                    Vector<TResult> result = operation.Apply(x, y, in dataChunk);
                    System.Runtime.CompilerServices.Unsafe.WriteUnaligned(destinationRowOffset + x, result);
                }

                for (; x < width; x++)
                {
                    *(destinationRowOffset + x) = operation.Apply(x, y, in *(sourceRowOffset + x));
                }
            });
        }

        /// <summary>
        /// Executes an operation that combines elements from two source maps into a destination map in parallel. 
        /// </summary>
        /// <typeparam name="TSource1">The type of elements in the first source map.</typeparam>
        /// <typeparam name="TSource2">The type of elements in the second source map.</typeparam>
        /// <typeparam name="TResult">The type of elements in the destination map.</typeparam>
        /// <typeparam name="TOperation">The type of the dual-source mapping operation. </typeparam>
        /// <param name="source1">The first source <see cref="UnmanagedMap{TSource1}"/>.</param>
        /// <param name="source2">The second source <see cref="UnmanagedMap{TSource2}"/>.</param>
        /// <param name="destination">The destination <see cref="UnmanagedMap{TResult}"/>.</param>
        /// <param name="operation">The operation to apply to elements from both sources.</param>
        /// <exception cref="ArgumentNullException">Thrown when any input map is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when map dimensions are mismatched.</exception>
        public static unsafe void ForEach<TSource1, TSource2, TResult, TOperation>(
            this UnmanagedMap<TSource1> source1,
            UnmanagedMap<TSource2> source2,
            UnmanagedMap<TResult> destination,
            TOperation operation)
            where TSource1 : unmanaged
            where TSource2 : unmanaged
            where TResult : unmanaged
            where TOperation : struct, IMapOperation<TSource1, TSource2, TResult>
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source1, nameof(source1));
            ObjectDisposedException.ThrowIf(source1.disposed, source1);

            ArgumentNullException.ThrowIfNull(source2, nameof(source2));
            ObjectDisposedException.ThrowIf(source2.disposed, source2);

            ArgumentNullException.ThrowIfNull(destination, nameof(destination));
            ObjectDisposedException.ThrowIf(destination.disposed, destination);

            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Width, source2.Width, nameof(source2));
            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Height, source2.Height, nameof(source2));

            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Width, destination.Width, nameof(destination));
            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Height, destination.Height, nameof(destination));
#else
            if (source1 is null)
            {
                throw new ArgumentNullException(nameof(source1));
            }
            if (source1.disposed)
            {
                throw new ObjectDisposedException(source1.GetType().FullName);
            }

            if (source2 is null)
            {
                throw new ArgumentNullException(nameof(source2));
            }
            if (source2.disposed)
            {
                throw new ObjectDisposedException(source2.GetType().FullName);
            }

            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (destination.disposed)
            {
                throw new ObjectDisposedException(destination.GetType().FullName);
            }

            if (source1.Width != destination.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected width {source1.Width}, but got {destination.Width}.");
            }
            if (source1.Height != destination.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected height {source1.Height}, but got {destination.Height}.");
            }

            if (source1.Width != source2.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(source2), $"Expected width {source1.Width}, but got {source2.Width}.");
            }
            if (source1.Height != source2.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(source2), $"Expected height {source1.Height}, but got {source2.Height}.");
            }
#endif
            int width = source1.Width;
            int height = source1.Height;
            TSource1* source1Buffer = source1.buffer;
            TSource2* source2Buffer = source2.buffer;
            TResult* destinationBuffer = destination.buffer;
            Parallel.For(0, height, y =>
            {
                TSource1* source1RowOffset = source1Buffer + (y * width);
                TSource2* source2RowOffset = source2Buffer + (y * width);
                TResult* destinationRowOffset = destinationBuffer + (y * width);
                for (int x = 0; x < width; x++)
                {
                    *(destinationRowOffset + x) = operation.Apply(x, y, in *(source1RowOffset + x), in *(source2RowOffset + x));
                }
            });
        }

        /// <summary>
        /// Performs a high-performance SIMD mapping operation combining two source maps into a destination map in parallel. 
        /// </summary>
        /// <typeparam name="TSource1">The type of elements in the first source map.</typeparam>
        /// <typeparam name="TSource2">The type of elements in the second source map.</typeparam>
        /// <typeparam name="TResult">The type of elements in the destination map.</typeparam>
        /// <typeparam name="TOperation">The type of the SIMD dual-source mapping operation. </typeparam>
        /// <param name="source1">The first source <see cref="UnmanagedMap{TSource1}"/>.</param>
        /// <param name="source2">The second source <see cref="UnmanagedMap{TSource2}"/>.</param>
        /// <param name="destination">The destination <see cref="UnmanagedMap{TResult}"/>.</param>
        /// <param name="operation">The SIMD operation to apply.</param>
        /// <exception cref="ArgumentException">Thrown when vector sizes of source and result types are inconsistent.</exception>
        /// <exception cref="ArgumentNullException">Thrown when any input map is <see langword="null"/>.</exception>
        public static unsafe void FastForEach<TSource1, TSource2, TResult, TOperation>(
            this UnmanagedMap<TSource1> source1,
            UnmanagedMap<TSource2> source2,
            UnmanagedMap<TResult> destination,
            TOperation operation)
            where TSource1 : unmanaged
            where TSource2 : unmanaged
            where TResult : unmanaged
            where TOperation : struct, ISimdMapOperation<TSource1, TSource2, TResult>
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(source1, nameof(source1));
            ObjectDisposedException.ThrowIf(source1.disposed, source1);

            ArgumentNullException.ThrowIfNull(source2, nameof(source2));
            ObjectDisposedException.ThrowIf(source2.disposed, source2);

            ArgumentNullException.ThrowIfNull(destination, nameof(destination));
            ObjectDisposedException.ThrowIf(destination.disposed, destination);

            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Width, source2.Width, nameof(source2));
            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Height, source2.Height, nameof(source2));

            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Width, destination.Width, nameof(destination));
            ArgumentOutOfRangeException.ThrowIfNotEqual(source1.Height, destination.Height, nameof(destination));
#else
            if (source1 is null)
            {
                throw new ArgumentNullException(nameof(source1));
            }
            if (source1.disposed)
            {
                throw new ObjectDisposedException(source1.GetType().FullName);
            }

            if (source2 is null)
            {
                throw new ArgumentNullException(nameof(source2));
            }
            if (source2.disposed)
            {
                throw new ObjectDisposedException(source2.GetType().FullName);
            }

            if (destination is null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (destination.disposed)
            {
                throw new ObjectDisposedException(destination.GetType().FullName);
            }

            if (source1.Width != destination.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected width {source1.Width}, but got {destination.Width}.");
            }
            if (source1.Height != destination.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(destination), $"Expected height {source1.Height}, but got {destination.Height}.");
            }

            if (source1.Width != source2.Width)
            {
                throw new ArgumentOutOfRangeException(nameof(source2), $"Expected width {source1.Width}, but got {source2.Width}.");
            }
            if (source1.Height != source2.Height)
            {
                throw new ArgumentOutOfRangeException(nameof(source2), $"Expected height {source1.Height}, but got {source2.Height}.");
            }
#endif
            int width = source1.Width;
            int height = source1.Height;
            TSource1* source1Buffer = source1.buffer;
            TSource2* source2Buffer = source2.buffer;
            TResult* destinationBuffer = destination.buffer;
            int vectorSize = Vector<TSource1>.Count;
            if (vectorSize != Vector<TSource2>.Count)
            {
                throw new ArgumentException($"Vector size mismatch: {typeof(TSource1)} has vector size {vectorSize}, while {typeof(TSource2)} has vector size {Vector<TSource2>.Count}.", nameof(operation));
            }
            if (vectorSize != Vector<TResult>.Count)
            {
                throw new ArgumentException($"Vector size mismatch: {typeof(TSource1)} has vector size {vectorSize}, while {typeof(TResult)} has vector size {Vector<TResult>.Count}.", nameof(operation));
            }
            Parallel.For(0, height, y =>
            {
                TSource1* source1RowOffset = source1Buffer + (y * width);
                TSource2* source2RowOffset = source2Buffer + (y * width);
                TResult* destinationRowOffset = destinationBuffer + (y * width);
                int x = 0;

                for (; x <= width - vectorSize; x += vectorSize)
                {
                    TSource1* source1Ptr = source1RowOffset + x;
                    TSource2* source2Ptr = source2RowOffset + x;
                    Vector<TSource1> source1DataChunk = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Vector<TSource1>>(source1Ptr);
                    Vector<TSource2> source2DataChunk = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<Vector<TSource2>>(source2Ptr);
                    Vector<TResult> result = operation.Apply(x, y, in source1DataChunk, in source2DataChunk);
                    System.Runtime.CompilerServices.Unsafe.WriteUnaligned(destinationRowOffset + x, result);
                }

                for (; x < width; x++)
                {
                    *(destinationRowOffset + x) = operation.Apply(x, y, in *(source1RowOffset + x), in *(source2RowOffset + x));
                }
            });
        }
    }
}