namespace GameSharp.Collections.Tests.TypeLookup;

public sealed class Aeroplane : IVehicle
{
    public static Aeroplane Default { get; } = new();

    public int WheelCount => 3;

    public override string ToString() => nameof(Aeroplane);
}