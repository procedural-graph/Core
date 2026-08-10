namespace GameSharp.ProceduralGraph.Collections;

/// <summary>
/// Defines a contract for objects that support creating both shallow and deep copies of themselves, enabling flexible
/// duplication strategies.
/// </summary>
/// <typeparam name="TSelf">The type of the object that implements this interface.</typeparam>
public interface ICloneable<TSelf> : System.ICloneable where TSelf : notnull
{
    /// <summary>
    /// Creates a shallow copy of the current instance.
    /// </summary>
    /// <returns>
    /// A new object that is a shallow copy of the current instance. The copy contains the same values for all fields
    /// and properties, but reference-type fields refer to the same objects as those in the original instance.
    /// </returns>
    TSelf ShallowCopy();

    /// <summary>
    /// Creates a deep copy of the current object, duplicating all nested objects to ensure the copy is independent of
    /// the original.
    /// </summary>
    /// <returns>
    /// A new object that is a deep copy of the current instance. Modifications to the returned object do not affect the
    /// original object.
    /// </returns>
    TSelf DeepCopy();

#if !NETFRAMEWORK
    object System.ICloneable.Clone() => DeepCopy();
#endif
}
