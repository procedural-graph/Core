using GameSharp.Sandbox.Generic.Windows;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace GameSharp.Sandbox.Windows;

#if NET9_0_OR_GREATER
using Lock = System.Threading.Lock;
#else
using Lock = object;
#endif

internal sealed partial class AppContainerProfile(string displayName, string description) : Disposable
{
    private static readonly int _processID =
#if NET5_0_OR_GREATER
        Environment.ProcessId;
#else
        System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
    private static int _lastProfileID = int.MinValue;

    private readonly string _name = $"GS_{(((long)_processID) << 32) | (uint)Interlocked.Increment(ref _lastProfileID):X16}";
    private readonly string _displayName = displayName;
    private readonly string _description = description;
    private readonly Lock _syncRoot = new();
    private volatile bool _created;

    public SafeSecurityIdentifierHandle GetSecurityIdentifier()
    {
        if (TryGetExistingSid(out SafeSecurityIdentifierHandle? sid))
        {
            return sid;
        }

        lock (_syncRoot)
        {
            if (TryGetExistingSid(out sid))
            {
                return sid;
            }

            return new SafeSecurityIdentifierHandle(SidFromNew(), ownsHandle: true);
        }
    }

#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    private bool TryGetExistingSid([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SafeSecurityIdentifierHandle? sid)
#else
    private bool TryGetExistingSid(out SafeSecurityIdentifierHandle sid)
#endif
    {
#if NET7_0_OR_GREATER
        ObjectDisposedException.ThrowIf(Disposed, this);
#else
        if (Disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
#endif

        if (_created)
        {
            sid = new SafeSecurityIdentifierHandle(SidFromExisting(), ownsHandle: true);
            return true;
        }

        sid = null!;
        return false;
    }

    protected override void OnDisposed()
    {
        if (!_created)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (!_created)
            {
                return;
            }

            _ = DeleteAppContainerProfile(_name);
            _created = false;
        }
    }

    private nint SidFromNew()
    {
        int hr = CreateAppContainerProfile(_name!, _displayName, _description, IntPtr.Zero, 0, out nint ppSid);

        switch (hr)
        {
            case 0x00000000: break; // S_OK
            case unchecked((int)0x800700B7): return SidFromExisting(); // ERROR_ALREADY_EXISTS
            default: Marshal.ThrowExceptionForHR(hr); break;
        }

        return ppSid;
    }

    private nint SidFromExisting()
    {
        int hr = DeriveAppContainerSidFromAppContainerName(_name!, out nint ppSid);

        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return ppSid;
    }

#if NET7_0_OR_GREATER
    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int CreateAppContainerProfile(
#else
        [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    private static extern int CreateAppContainerProfile(
#endif
        string pszAppContainerName,
        string pszDisplayName,
        string pszDescription,
        IntPtr pCapabilities,
        uint dwCapabilityCount,
        out IntPtr ppSid);

#if NET7_0_OR_GREATER
    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int DeriveAppContainerSidFromAppContainerName(
#else
    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    private static extern int DeriveAppContainerSidFromAppContainerName(
#endif
        string pszAppContainerName,
        out IntPtr ppSid);

#if NET7_0_OR_GREATER
    [LibraryImport("userenv.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int DeleteAppContainerProfile(string pszAppContainerName);
#else
    [DllImport("userenv.dll", CharSet = CharSet.Unicode)]
    private static extern int DeleteAppContainerProfile(string pszAppContainerName);
#endif
}
