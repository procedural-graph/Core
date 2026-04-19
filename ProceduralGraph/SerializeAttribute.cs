using System;

namespace ProceduralGraph;

/// <summary>
/// Specifies that a property, field, class, or struct should be included in serialization and deserialization processes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class SerializeAttribute : Attribute;
