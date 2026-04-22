namespace ProceduralGraph;

/// <summary>
/// Defines a contract for creating terrain instances.
/// </summary>
public interface ITerrainProvider
{
    /// <summary>
    /// Creates a new terrain instance.
    /// </summary>
    /// <returns>The created terrain instance.</returns>
    public ITerrain CreateInstance();
}
