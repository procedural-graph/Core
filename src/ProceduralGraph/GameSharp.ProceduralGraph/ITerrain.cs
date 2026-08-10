using GameSharp.ProceduralGraph.Collections.Unsafe;
using GameSharp.ProceduralGraph.Mathematics;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameSharp.ProceduralGraph;

/// <summary>
/// Defines the contract for a terrain object within a scene.
/// </summary>
public interface ITerrain : ISceneMember
{
    /// <summary>
    /// Gets the dimensions of the terrain.
    /// </summary>
    Int2 Size { get; }

    /// <summary>
    /// Asynchronously rebuilds the terrain data using the specified size, height map, and splat weight maps.
    /// </summary>
    /// <param name="heights">A writable map containing the height values for each terrain cell.</param>
    /// <param name="splatWeights">
    /// A read-only memory of splat weight maps, where each map represents the blending weights for a terrain texture
    /// layer. The number of maps determines the number of texture layers.
    /// </param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="ValueTask"/> that represents the asynchronous rebuild operation.</returns>
    ValueTask RebuildAsync(UnmanagedMap<float> heights, ReadOnlyMemory<UnmanagedMap<Pixel32>> splatWeights, CancellationToken cancellationToken);
}
