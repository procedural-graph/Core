namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class Bicycle : IVehicle
{
    public static Bicycle Default { get; } = new();

    public int WheelCount => 2;

    public override string ToString() => nameof(Bicycle);
}
