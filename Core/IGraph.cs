namespace ProceduralGraph
{
    /// <summary>
    /// Defines the contract for a graph instance that supports asynchronous lifecycle management, graph element
    /// serialization, and diagnostic logging.
    /// </summary>
    public interface IGraph : IAsyncLifecycle
    {
        /// <summary>
        /// Gets the collection of graph converters used to serialize and deserialize graph elements.
        /// </summary>
        IGraphConverterProvider Converters { get; }

        /// <summary>
        /// Gets the logger instance used to record diagnostic and operational messages for the current node.
        /// </summary>
        ILogger Logger { get; }
    }
}
