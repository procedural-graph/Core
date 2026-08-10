using System;

namespace GameSharp.Sandbox.Dotnet;

/// <summary>
/// Provides extension methods for the <see cref="HostBuilder"/> class to add .NET process factory support.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Adds .NET process factory support to the <see cref="HostBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="HostBuilder"/> to add .NET process factory support to.</param>
    /// <returns>The <see cref="HostBuilder"/> with .NET process factory support added.</returns>
#if NET7_0_OR_GREATER
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static HostBuilder AddDotnet(this HostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
#else
    public static HostBuilder AddDotnet(this HostBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }
#endif
        return builder.AddProcessFactoryProvider(new DotnetProcessFactoryProvider());
    }
}
