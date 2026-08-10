using System;
using System.Runtime.CompilerServices;
#if NET7_0_OR_GREATER
using System.IO;
#else
using System.Runtime.InteropServices;
#endif

namespace GameSharp.Sandbox;

internal sealed class NativeProcessFactory : ProcessFactory
{
    public override bool TryConfigure<TArgs>(string assemblyPath, ref TArgs args)
    {
        if (Platform.IsWindows())
        {
            return ConfigureIf(assemblyPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase), assemblyPath, ref args);
        }

#if NET7_0_OR_GREATER
#pragma warning disable CA1416 // Validate platform compatibility
        UnixFileMode mode = File.GetUnixFileMode(assemblyPath);
#pragma warning restore CA1416 // Validate platform compatibility
        return ConfigureIf((mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != 0, assemblyPath, ref args);
    }
#else
        return ConfigureIf(access(assemblyPath, 1) == 0, assemblyPath, ref args);
    }

    [DllImport("libc", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern int access(string pathname, int mode);
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ConfigureIf<TArgs>(bool condition, string assemblyPath, ref TArgs args) where TArgs : struct, ICommandLineArguments
    {
        if (condition)
        {
            args.AssemblyPath = assemblyPath;
            return true;
        }

        return false;
    }
}
