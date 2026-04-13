using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace ProceduralGraph.Collections;

internal readonly struct TypeRegistration(int id) : IEquatable<TypeRegistration>
{
    public int ID { get; init; } = id;

    public int Order { get; init; }

    public ImmutableArray<int> DerivedTypes { get; init; } = [id];

    public bool Equals(TypeRegistration other) => ID == other.ID;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is TypeRegistration other && Equals(other);

    public override int GetHashCode() => ID.GetHashCode();

    public void Deconstruct(out int id, out int order, out ImmutableArray<int> derivedTypes)
    {
        id = ID;
        order = Order;
        derivedTypes = DerivedTypes;
    }

    public static bool operator ==(TypeRegistration left, TypeRegistration right) => left.Equals(right);

    public static bool operator !=(TypeRegistration left, TypeRegistration right) => !left.Equals(right);
}
