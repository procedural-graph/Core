using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace GameSharp.Sandbox.Generic.Windows;

internal sealed partial class ProcessAttributeListSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public ProcessAttributeListSafeHandle(nint handle) : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    protected override unsafe bool ReleaseHandle()
    {
        void* ptr = (void*)handle;
        DeleteProcThreadAttributeList(ptr);
        Marshal.FreeHGlobal((nint)ptr);
        return true;
    }

#if NET7_0_OR_GREATER
    [LibraryImport("Kernel32.dll")]
    private static unsafe partial void DeleteProcThreadAttributeList(void* lpAttributeList);
#else
    [DllImport("Kernel32.dll")]
    private static unsafe extern void DeleteProcThreadAttributeList(void* lpAttributeList);
#endif
}
