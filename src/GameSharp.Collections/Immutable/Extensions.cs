namespace GameSharp.Collections.Immutable;

/// <summary>
/// Provides extension methods for converting mutable <see cref="TypeLookup"/> instances to immutable <see cref="ImmutableTypeLookup"/> instances.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Converts an immutable <see cref="ReadOnlyTypeLookup"/> instance to a mutable <see cref="ImmutableTypeLookup.Builder"/> instance.
    /// </summary>
    /// <param name="values">The immutable <see cref="ReadOnlyTypeLookup"/> instance to convert.</param>
    /// <returns>A mutable <see cref="ImmutableTypeLookup.Builder"/> instance containing the same data as the input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="values"/> parameter is null.</exception>
    public static ImmutableTypeLookup.Builder AsImmutableBuilder(this ReadOnlyTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return [];
        }

        return new ImmutableTypeLookup.Builder(values.Lookups.Span, values.Items.Span);
    }

    /// <summary>
    /// Converts a mutable <see cref="TypeLookup"/> instance to an immutable <see cref="ImmutableTypeLookup"/> instance.
    /// </summary>
    /// <param name="values">The mutable <see cref="TypeLookup"/> instance to convert.</param>
    /// <returns>An immutable <see cref="ImmutableTypeLookup"/> instance containing the same data as the input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="values"/> parameter is null.</exception>
    public static ImmutableTypeLookup AsImmutableTypeLookup(this TypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return [];
        }

        ReadOnlyTypeLookup.IntegerLookup[] lookups = [.. values.Lookups.Span];
        object[] items = [.. values.Items.Span];

        return new ImmutableTypeLookup(lookups, lookups.Length, items, items.Length);
    }

    /// <summary>
    /// Converts an immutable <see cref="ImmutableTypeLookup"/> instance to a mutable <see cref="TypeLookup"/> instance.
    /// </summary>
    /// <param name="values">The immutable <see cref="ImmutableTypeLookup"/> instance to convert.</param>
    /// <returns>A mutable <see cref="TypeLookup"/> instance containing the same data as the input.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="values"/> parameter is null.</exception>
    public static TypeLookup AsTypeLookup(this ImmutableTypeLookup values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
        {
            return [];
        }

        ReadOnlyTypeLookup.IntegerLookup[] lookups = [.. values.Lookups.Span];
        object[] items = [.. values.Items.Span];

        return new TypeLookup(lookups, lookups.Length, items, items.Length);
    }
}
