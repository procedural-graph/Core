using System;

namespace ProceduralGraph;

/// <summary>
/// Specifies that a property, field, class, or struct should be included in serialization and deserialization processes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class SerializeAttribute : Attribute;

/// <summary>
/// Specifies that a property or field should be populated by dependency injection with the specified service type.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class InjectAttribute : Attribute
{
    /// <summary>
    /// Gets the type of service to inject. If <see langword="null"/>, the member's type will be used.
    /// </summary>
    public Type? ServiceType { get; init; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InjectAttribute"/> class.
    /// </summary>
    public InjectAttribute() { }

    /// <inheritdoc cref="InjectAttribute()"/>
    /// <param name="serviceType">The type of service to inject.</param>
    public InjectAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }
}
