using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32.System.Threading;
using System;
using Windows.Win32.System.JobObjects;
using Windows.Win32.Foundation;
using System.Threading.Tasks;
using System.Threading;

#if NET6_0_OR_GREATER
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
#endif

namespace GameSharp.Sandbox.Generic.Windows;

internal static partial class Win32
{
    private const string Kernel32 = "kernel32.dll";

    public static uint ResumeThread(SafeProcessHandle hThread)
    {
        uint result = ResumeThreadImpl(hThread);
        if (result == unchecked((uint)-1))
        {
#if NET6_0_OR_GREATER
            ThrowLastWin32Exception();
#else
            throw LastWin32Exception();
#endif
        }
        return result;
    }
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "ResumeThread", SetLastError = true)]
    private static partial uint ResumeThreadImpl(SafeProcessHandle hThread);
#else
    [DllImport(Kernel32, EntryPoint = "ResumeThread", SetLastError = true)]
    private static extern uint ResumeThreadImpl(SafeProcessHandle hThread);
#endif

    public static unsafe PROCESS_INFORMATION CreateProcess(in STARTUPINFOEXW startupInfo, string commandLine, uint creationFlags = 0, bool inheritHandles = false)
    {
        PROCESS_INFORMATION processInformation;
        bool success;
        fixed (STARTUPINFOEXW* startupInfoPtr = &startupInfo)
        {
            success = TryCreateProcess(
                lpApplicationName: null,
                lpCommandLine: commandLine,
                lpProcessAttributes: 0,
                lpThreadAttributes: 0,
                bInheritHandles: inheritHandles,
                dwCreationFlags: creationFlags,
                lpEnvironment: 0,
                lpCurrentDirectory: null,
                lpStartupInfo: (nint)startupInfoPtr,
#if NET7_0_OR_GREATER
                &processInformation);
#else
                out processInformation);
#endif
        }
        if (success)
        {
            return processInformation;
        }
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
        return default!;
#else
        throw LastWin32Exception();
#endif
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static unsafe partial bool TryCreateProcess(
#else
    [DllImport(Kernel32, EntryPoint = "CreateProcessW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern bool TryCreateProcess(
#endif
        string? lpApplicationName,
        string lpCommandLine,
        nint lpProcessAttributes,
        nint lpThreadAttributes,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        string? lpCurrentDirectory,
        nint lpStartupInfo,
#if NET7_0_OR_GREATER
        PROCESS_INFORMATION* lpProcessInformation);
#else
        out PROCESS_INFORMATION lpProcessInformation);
#endif

#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static long GetProcessExitCode(SafeProcessHandle hProcess)
    {
        if (!TryGetProcessExitCode(hProcess, out uint exitCode))
        {
#if NET6_0_OR_GREATER
            ThrowLastWin32Exception();
#else
            throw LastWin32Exception();
#endif
        }
        
        return exitCode;
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "GetExitCodeProcess", SetLastError = true)]
    private static partial bool TryGetProcessExitCode(SafeProcessHandle hProcess, out uint lpExitCode);
#else
    [DllImport(Kernel32, EntryPoint = "GetExitCodeProcess", SetLastError = true)]
    private static extern bool TryGetProcessExitCode(SafeProcessHandle hProcess, out uint lpExitCode);
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task WaitForSingleObjectAsync(SafeProcessHandle hHandle, CancellationToken cancellationToken = default)
    {
        return Task.Factory.StartNew(
            static state => WaitForSingleObject((SafeProcessHandle)state!), 
            hHandle, 
            cancellationToken, 
            TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning, 
            TaskScheduler.Default);
    }
#if NET7_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void WaitForSingleObject(SafeProcessHandle hHandle)
    {
        if (WaitForSingleObjectImpl(hHandle, 0xFFFFFFFF) == (uint)WAIT_EVENT.WAIT_FAILED)
        {
#if NET6_0_OR_GREATER
            ThrowLastWin32Exception();
#else
            throw LastWin32Exception();
#endif
        }
    }
    public static bool WaitForSingleObject(SafeProcessHandle hHandle, TimeSpan timeout)
    {
        switch ((WAIT_EVENT)WaitForSingleObjectImpl(hHandle, (uint)timeout.TotalMilliseconds))
        {
            case WAIT_EVENT.WAIT_OBJECT_0: return true;
            case WAIT_EVENT.WAIT_ABANDONED:
            case WAIT_EVENT.WAIT_TIMEOUT: return false;
#if NET6_0_OR_GREATER
            default: ThrowLastWin32Exception(); return false;
#else
            default: throw LastWin32Exception();
#endif
        }
    }
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static partial uint WaitForSingleObjectImpl(SafeProcessHandle hHandle, uint dwMilliseconds);
#else
    [DllImport(Kernel32, EntryPoint = "WaitForSingleObject", SetLastError = true)]
    private static extern uint WaitForSingleObjectImpl(SafeProcessHandle hHandle, uint dwMilliseconds);
#endif

    public static string PathToExecutable(SafeProcessHandle hProcess)
    {
        Span<char> buffer = stackalloc char[260]; // MAX_PATH
        uint size = (uint)buffer.Length;
        if (QueryFullProcessImageNameW(hProcess, 0, buffer, ref size))
        {
            return buffer.Slice(0, (int)size).ToString();
        }

        int hr = Marshal.GetLastWin32Error();
        if (hr != 0x7A) // ERROR_INSUFFICIENT_BUFFER
        {
            Marshal.ThrowExceptionForHR(hr);
            return null!;
        }

#if NETFRAMEWORK
        string path = new('\0', (int)size);
        unsafe
        {
            fixed (char* pathPtr = path)
            {
                if (QueryFullProcessImageNameW(hProcess, 0, new Span<char>(pathPtr, path.Length), ref size))
                {
                    return path;
                }
            }
        }
        hr = Marshal.GetLastWin32Error();
        Marshal.ThrowExceptionForHR(hr);
        return null!;
#else
        string path = string.Create((int)size, hProcess, static (span, processHandle) =>
        {
            uint size = (uint)span.Length;
            if (!QueryFullProcessImageNameW(processHandle, 0, span, ref size))
            {
                int hr = Marshal.GetLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        });
        return path;
#endif
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial bool QueryFullProcessImageNameW(SafeProcessHandle hProcess, uint dwFlags, Span<char> lpExeName, ref uint lpdwSize);
#else
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageNameW(SafeProcessHandle hProcess, uint dwFlags, Span<char> lpExeName, ref uint lpdwSize);
#endif

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static int GetProcessID(SafeProcessHandle hProcess)
    {
        uint processId = GetProcessIDImpl(hProcess);
        if (processId == 0)
        {
#if NET6_0_OR_GREATER
            ThrowLastWin32Exception();
            return default!;
#else
            throw LastWin32Exception();
#endif
        }
        return (int)processId;
    }
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "GetProcessId", SetLastError = true)]
    private static partial uint GetProcessIDImpl(SafeProcessHandle hProcess);
#else
    [DllImport(Kernel32, EntryPoint = "GetProcessId", SetLastError = true)]
    private static extern uint GetProcessIDImpl(SafeProcessHandle hProcess);
#endif

    public static SafeProcessHandle CreateJobObject(nint lpJobAttributes = 0, string? lpName = null)
    {
        if (CreateJobObjectImpl(lpJobAttributes, lpName) is { } handle)
        {
            return handle;
        }
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
        return default!;
#else
        throw LastWin32Exception();
#endif
    }
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "CreateJobObjectW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    private static partial SafeProcessHandle? CreateJobObjectImpl(nint lpJobAttributes, string? lpName);
#else
    [DllImport(Kernel32, EntryPoint = "CreateJobObjectW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeProcessHandle? CreateJobObjectImpl(nint lpJobAttributes, string? lpName);
#endif

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static unsafe void SetJobObjectInformation<T>(SafeProcessHandle handle, JOBOBJECTINFOCLASS jobObjectInformationClass, in T value) where T : unmanaged
    {
        bool success;
        fixed (T* valuePtr = &value)
        {
            success = TrySetJobInformation(handle, (int)jobObjectInformationClass, (nint)valuePtr, (uint)sizeof(T));
        }
        if (success)
        {
            return;
        }
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
#else
        throw LastWin32Exception();
#endif
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "SetInformationJobObject", SetLastError = true)]
    private static partial bool TrySetJobInformation(SafeProcessHandle hJob, int JobObjectInformationClass, nint lpJobObjectInformation, uint cbJobObjectInformationLength);
#else
    [DllImport(Kernel32, EntryPoint = "SetInformationJobObject", SetLastError = true)]
    private static extern bool TrySetJobInformation(SafeProcessHandle hJob, int JobObjectInformationClass, nint lpJobObjectInformation, uint cbJobObjectInformationLength);
#endif

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void AssignProcessToJobObject(SafeProcessHandle hJob, SafeProcessHandle hProcess)
    {
        if (TryAssignProcessToJobObject(hJob, hProcess))
        {
            return;
        }
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
#else
        throw LastWin32Exception();
#endif
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "AssignProcessToJobObject", SetLastError = true)]
    private static partial bool TryAssignProcessToJobObject(SafeProcessHandle hJob, SafeProcessHandle hProcess);
#else
    [DllImport(Kernel32, EntryPoint = "AssignProcessToJobObject", SetLastError = true)]
    private static extern bool TryAssignProcessToJobObject(SafeProcessHandle hJob, SafeProcessHandle hProcess);
#endif

    public static unsafe ProcessAttributeListSafeHandle CreateProcessAttributeList(uint dwAttributeCount, uint dwFlags = 0u)
    {
        Unsafe.SkipInit(out nint lpSize);

        TryCreateProcessAttributeListImpl(null, dwAttributeCount, dwFlags, ref lpSize);
        int hr = Marshal.GetLastWin32Error();
        if (hr != 0x7A) // ERROR_INSUFFICIENT_BUFFER
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        ProcessAttributeListSafeHandle lpAttributeList = new(Marshal.AllocHGlobal(lpSize));
        if (TryCreateProcessAttributeListImpl((void*)lpAttributeList.DangerousGetHandle(), dwAttributeCount, dwFlags, ref lpSize))
        {
            return lpAttributeList;
        }

        lpAttributeList.Dispose();
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
        return null;
#else
        throw LastWin32Exception();
#endif
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "InitializeProcThreadAttributeList", SetLastError = true)]
    private static unsafe partial bool TryCreateProcessAttributeListImpl(void* lpAttributeList, uint dwAttributeCount, uint dwFlags, ref nint lpSize);
#else
    [DllImport(Kernel32, EntryPoint = "InitializeProcThreadAttributeList", SetLastError = true)]
    private static extern unsafe bool TryCreateProcessAttributeListImpl(void* lpAttributeList, uint dwAttributeCount, uint dwFlags, ref nint lpSize);
#endif

    public static unsafe void UpdateProcessThreadAttribute<T>(ProcessAttributeListSafeHandle lpAttributeList, nint attribute, in T value) where T : unmanaged
    {
        fixed (T* valuePtr = &value)
        {
            if (TryUpdateProcessThreadAttribute(lpAttributeList, 0u, attribute, (IntPtr)valuePtr, sizeof(T), IntPtr.Zero, IntPtr.Zero))
            {
                return;
            }
        }
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
#else
        throw LastWin32Exception();
#endif
    }

    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "UpdateProcThreadAttribute", SetLastError = true)]
    private static partial bool TryUpdateProcessThreadAttribute(
#else
    [DllImport(Kernel32, EntryPoint = "UpdateProcThreadAttribute", SetLastError = true)]
    private static extern bool TryUpdateProcessThreadAttribute(
#endif
        ProcessAttributeListSafeHandle lpAttributeList,
        uint dwFlags,
        IntPtr Attribute,
        IntPtr lpValue,
        nint cbSize,
        IntPtr lpPreviousValue,
        IntPtr lpReturnSize);

#if NET6_0_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    public static void CloseHandle(nint hObject)
    {
        if (TryCloseHandle(hObject))
        {
            return;
        }
#if NET6_0_OR_GREATER
        ThrowLastWin32Exception();
#else
        throw LastWin32Exception();
#endif
    }
    [return: MarshalAs(UnmanagedType.Bool)]
#if NET7_0_OR_GREATER
    [LibraryImport(Kernel32, EntryPoint = "CloseHandle", SetLastError = true)]
    private static partial bool TryCloseHandle(nint hObject);
#else
    [DllImport(Kernel32, EntryPoint = "CloseHandle", SetLastError = true)]
    private static extern bool TryCloseHandle(nint hObject);
#endif

#if NET6_0_OR_GREATER
    [DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowLastWin32Exception()
    {
        throw LastWin32Exception();
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Win32Exception LastWin32Exception()
    {
        int error = Marshal.GetLastWin32Error();
        return new Win32Exception(error);
    }
}
