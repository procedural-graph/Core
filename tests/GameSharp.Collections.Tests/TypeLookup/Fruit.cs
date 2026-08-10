namespace GameSharp.Collections.Tests.TypeLookup;

public class Fruit
{
    public static Fruit Default { get; } = new();

    public override string ToString() => GetType().Name;
}