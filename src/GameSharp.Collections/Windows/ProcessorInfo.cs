using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.SystemInformation;

namespace GameSharp.Collections.Windows;

internal sealed partial class ProcessorInfo : Collections.ProcessorInfo
{
    public override int LineCacheSizeInBytes { get; }

    public unsafe ProcessorInfo()
    {
        const int ERROR_INSUFFICIENT_BUFFER = 122;

        uint returnLength = 0;

        GetLogicalProcessorInformation(nint.Zero, ref returnLength);
        int error = Marshal.GetLastWin32Error();
        if (error != ERROR_INSUFFICIENT_BUFFER)
        {
            ThrowLastWin32Exception(error);
        }

        nint firstElemPtr = Marshal.AllocHGlobal((int)returnLength);
        try
        {
            if (!GetLogicalProcessorInformation(firstElemPtr, ref returnLength))
            {
                ThrowLastWin32Exception(Marshal.GetLastWin32Error());
            }

            for (nint currElemPtr = firstElemPtr, boundaryPtr = firstElemPtr + (nint)returnLength; currElemPtr < boundaryPtr; currElemPtr++)
            {
                ref readonly SYSTEM_LOGICAL_PROCESSOR_INFORMATION info = ref *(SYSTEM_LOGICAL_PROCESSOR_INFORMATION*)currElemPtr;
                if (info.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationCache)
                {
                    LineCacheSizeInBytes = info.Anonymous.Cache.LineSize;
                    return;
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(firstElemPtr);
        }

        LineCacheSizeInBytes = base.LineCacheSizeInBytes;
    }

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetLogicalProcessorInformation(nint Buffer, ref uint ReturnLength);

    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowLastWin32Exception(int error)
    {
        throw new Win32Exception(error);
    }
}
