using System.Text.Json;

namespace ProceduralGraph;

/// <summary>
/// Defines a builder for configuring and constructing an application instance.
/// </summary>
public interface IApplicationBuilder
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used for serializing and deserializing graph data.
    /// </summary>
    JsonSerializerOptions JsonSerializerOptions { get; }

    /// <summary>
    /// Builds the application using the configured services and options, returning an <see cref="AsyncLifecycle"/> that represents the application's lifetime.
    /// </summary>
    /// <remarks>This method can only be called once.</remarks>
    /// <returns>An <see cref="AsyncLifecycle"/> that represents the application's lifetime.</returns>
    AsyncLifecycle Build();
}
