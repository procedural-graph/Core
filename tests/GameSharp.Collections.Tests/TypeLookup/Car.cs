namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class Car : IVehicle
{
    public static Car Default { get; } = new();

    public int WheelCount => 4;

    public override string ToString() => nameof(Car);
}
