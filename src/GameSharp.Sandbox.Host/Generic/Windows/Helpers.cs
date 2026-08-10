using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameSharp.Sandbox.Generic.Windows;

internal static class Helpers
{
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining), System.Diagnostics.StackTraceHidden]
#endif
    public static nint AddRefOrThrow(SafeHandle safeHandle)
    {
        Unsafe.SkipInit(out bool success);
        safeHandle.DangerousAddRef(ref success);
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(!success, safeHandle);
#else
        if (!success)
        {
            throw new ObjectDisposedException(safeHandle.GetType().FullName);
        }
#endif
        return safeHandle.DangerousGetHandle();
    }
}
