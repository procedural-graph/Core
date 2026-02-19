namespace ProceduralGraph.Generic
{
    /// <summary>
    /// Defines a contract for graph nodes that serve as proxies for scene members, allowing them to be integrated into the graph structure.
    /// </summary>
    /// <typeparam name="T">The engine-specific type of scene hierarchy member. Must be a reference type.</typeparam>
    public interface IProxyGraphNode<T> : IGraphNode where T : class
    {
        /// <summary>
        /// Gets the value associated with the scene member for the current instance.
        /// </summary>
        public T SceneMember { get; }
    }
}
