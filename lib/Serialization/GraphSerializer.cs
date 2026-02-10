using System.Collections;
using System.Threading.Channels;

namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Provides functionality to serialize a graph structure into a sequence of model objects using registered graph
    /// converters. Supports both synchronous and asynchronous enumeration of the serialized models.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GraphSerializer"/> traverses the graph in depth-first order, converting each node to a model
    /// object if a suitable converter is available.
    /// </para>
    /// <para>Thread safety is not guaranteed; use separate instances for concurrent operations.</para>
    /// </remarks>
    public readonly partial struct GraphSerializer : IEnumerable<object>, IAsyncEnumerable<object>
    {
        private struct DepthFirstEnumerator : IEnumerator<IGraphNode>
        {
            private readonly GraphSerializer _serializer;
            private readonly Stack<IGraphNode> _stack;

            private IGraphNode? _current;
            public readonly IGraphNode Current => _current!;
            readonly object IEnumerator.Current => Current;

            public DepthFirstEnumerator(GraphSerializer serializer)
            {
                _serializer = serializer;
                _stack = new Stack<IGraphNode>();
                _current = _serializer.Root;
            }

            public bool MoveNext()
            {
                IReadOnlyCollection<IGraphNode> descendants = _current!.Descendants;
                int descendantCount = descendants.Count;

                if (descendantCount > 0)
                {
#if NET8_0_OR_GREATER
                _stack.EnsureCapacity(_stack.Count + descendantCount);
#endif

                    foreach (IGraphNode descendant in descendants)
                    {
                        _stack.Push(descendant);
                    }
                }

                return _stack.TryPop(out _current);
            }

            public void Reset()
            {
                _current = _serializer.Root;
                _stack.Clear();
            }

            readonly void IDisposable.Dispose() { }
        }

        private partial class GraphSerializationContext : IDisposable
        {
            private bool _disposed;

            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1000, 1000);

            private void Dispose(bool disposing)
            {
                if (_disposed)
                {
                    return;
                }

                if (disposing)
                {
                    Semaphore.Dispose();
                }

                _disposed = true;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
            }
        }

        /// <summary>
        /// Gets the root node of the graph.
        /// </summary>
        public IGraphNode Root { get; }

        /// <summary>
        /// Gets the provider used to convert between graph elements and their serialized representations.
        /// </summary>
        public IGraphConverterProvider Converters { get; }

        /// <summary>
        /// Gets the asynchronous lifecycle host for the current instance.
        /// </summary>
        public IAsyncLifecycle Host { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="GraphSerializer"/> with the specified root node, converter provider,
        /// host lifecycle, and logger.
        /// </summary>
        /// <param name="root">
        /// The root graph node to be serialized. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="converters">
        /// The provider used to obtain graph converters for serialization. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <param name="host">
        /// The asynchronous lifecycle host that manages the serializer's lifetime. 
        /// Cannot be <see langword="null"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if root, converters, or host is <see langword="null"/>.</exception>
        public GraphSerializer(IGraphNode root, IGraphConverterProvider converters, IAsyncLifecycle host)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Converters = converters ?? throw new ArgumentNullException(nameof(converters));
            Host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <inheritdoc/>
        public IEnumerator<object> GetEnumerator()
        {
            using var enumerator = new DepthFirstEnumerator(this);
            while (enumerator.MoveNext())
            {
                IGraphNode current = enumerator.Current;
                if (Converters.TryFind(current, out IGraphConverter? converter))
                {
                    yield return converter.ToModel(current, Host);
                }
            }
        }

        /// <inheritdoc/>
        public async readonly IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Channel<Task<object>> channel = Channel.CreateUnbounded<Task<object>>();
            Task produce = ProduceModelsAsync(channel.Writer, cts.Token);
            await foreach (Task<object> task in channel.Reader.ReadAllAsync(cancellationToken))
            {
                object result;
                try
                {
                    result = await task;
                }
                catch
                {
                    cts.Cancel();
                    throw;
                }
                yield return result;
            }

            try
            {
                await produce;
            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
        }

        private readonly async Task ProduceModelsAsync(ChannelWriter<Task<object>> writer, CancellationToken cancellationToken)
        {
            await Task.Yield();

            using var enumerator = new DepthFirstEnumerator(this);

            var serializationContext = new GraphSerializationContext(writer)
            {
                count = 1
            };

            try
            {
                while (enumerator.MoveNext())
                {
                    IGraphNode current = enumerator.Current;
                    if (!Converters.TryFind(current, out IGraphConverter? converter))
                    {
                        continue;
                    }

                    await serializationContext.Semaphore.WaitAsync(cancellationToken);

                    Interlocked.Increment(ref serializationContext.count);
                    Task<object> serialize = SerializeNodeAsync(Host, current, converter);
                    _ = serialize.ContinueWith(
                        OnConversionCompleted,
                        serializationContext,
                        cancellationToken,
                        TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
            }
            finally
            {
                if (Interlocked.Decrement(ref serializationContext.count) == 0)
                {
                    writer.TryComplete();
                    serializationContext.Dispose();
                }
            }
        }

        private static void OnConversionCompleted(Task<object> task, object? obj)
        {
#if NET7_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(obj, nameof(obj));
#else
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
#endif
            GraphSerializationContext serializationContext = (GraphSerializationContext)obj;
            serializationContext.Semaphore.Release();
            ChannelWriter<Task<object>> writer = serializationContext.Writer;
            writer.TryWrite(task);
            if (Interlocked.Decrement(ref serializationContext.count) == 0)
            {
                writer.TryComplete();
                serializationContext.Dispose();
            }
        }

        private static async Task<object> SerializeNodeAsync(IAsyncLifecycle host, IGraphNode node, IGraphConverter converter)
        {
            await Task.Yield();
            return converter.ToModel(node, host);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
