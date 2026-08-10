using System.Runtime.CompilerServices;

namespace GameSharp;

/// <summary>
/// Provides information about the .NET runtime installation.
/// </summary>
public static class Platform
{
    /// <summary>
    /// Indicates whether the current application is running on Windows.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the current application is running on Windows; 
    /// <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWindows()
    {
#if NETFRAMEWORK
        return true;
#elif NET5_0_OR_GREATER
        return System.OperatingSystem.IsWindows();
#else
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);
#endif
    }

    /// <summary>
    /// Indicates whether the current application is running on Mac OS.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the current application is running on Mac OS; 
    /// <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMacOs()
    {
#if NETFRAMEWORK
        return false;
#elif NET5_0_OR_GREATER
        return System.OperatingSystem.IsMacOS();
#else
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX);
#endif
    }

    /// <summary>
    /// Indicates whether the current application is running on Linux.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the current application is running on Linux; 
    /// <see langword="false"/> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLinux()
    {
#if NETFRAMEWORK
        return false;
#elif NET5_0_OR_GREATER
        return System.OperatingSystem.IsLinux();
#else
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux);
#endif
    }
}
