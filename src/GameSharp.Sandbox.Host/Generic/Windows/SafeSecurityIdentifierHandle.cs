using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace GameSharp.Sandbox.Generic.Windows;

internal sealed partial class SafeSecurityIdentifierHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafeSecurityIdentifierHandle(nint psid, bool ownsHandle) : base(ownsHandle)
    {
        handle = psid;
    }

    protected override bool ReleaseHandle()
    {
        return FreeSid(handle) == default;
    }

#if NET7_0_OR_GREATER
    [LibraryImport("advapi32.dll")]
    public static partial nint FreeSid(nint pSid);
#else
    [DllImport("advapi32.dll")]
    public static extern nint FreeSid(nint pSid);
#endif
}
