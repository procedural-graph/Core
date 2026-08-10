using System;

namespace GameSharp.Sandbox.Java;

/// <summary>
/// Provides extension methods for the <see cref="HostBuilder"/> class to add Java process support.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds Java process support to the <see cref="HostBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="HostBuilder"/> to add Java process support to.</param>
    /// <returns>The <see cref="HostBuilder"/> with Java process support added.</returns>
#if NET7_0_OR_GREATER
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static HostBuilder AddJava(this HostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
#else
    public static HostBuilder AddJava(this HostBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
#endif
        return builder.AddProcessFactoryProvider(new JavaProcessFactoryProvider());
    }
}
