using Microsoft.Win32.SafeHandles;
using StreamJsonRpc;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace GameSharp.Sandbox.Generic.Windows;

[SuppressMessage("Interoperability", "CA1416", Justification = "Guarded by the static factory method on the Host class.")]
internal sealed class WindowsGuest : Guest<CommandLineArguments, WindowsGuest.ProcessExitInfo>
{
    internal readonly struct ProcessExitInfo : IDisposable
    {
        public long ExitCode { get; }
        public int ProcessID { get; }
        public StreamReader StdError { get; }

        internal ProcessExitInfo(long exitCode, int processID, AnonymousPipeServerStream stdErrorRead)
        {
            ExitCode = exitCode;
            ProcessID = processID;
            StdError = new StreamReader(stdErrorRead, Encoding.UTF8);
        }

        public void Dispose() => StdError?.Dispose();
    }

    private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
    private const uint CREATE_SUSPENDED = 0x00000004;

    private readonly SafeProcessHandle _threadHandle;
    private readonly SafeProcessHandle _processHandle;
    private readonly SafeSecurityIdentifierHandle _securityIdentifierHandle;
    private readonly AnonymousPipeServerStream _stdErrorRead;
    private readonly AnonymousPipeClientStream _stdErrorWrite;
    private readonly string _executablePath;

    public WindowsGuest(
        AnonymousPipeServerStream outboundPipe,
        AnonymousPipeServerStream inboundPipe,
        JsonRpc jsonRpc,
        SafeProcessHandle jobObjHandle,
        SafeSecurityIdentifierHandle securityIdentifierHandle,
        ProcessAttributeListSafeHandle processAttributeList, 
        scoped ref readonly CommandLineArguments args,
        WindowsHost host) : base(inboundPipe, outboundPipe, jsonRpc, host)
    {
        _executablePath = args.RuntimePath ?? args.AssemblyPath;
        _securityIdentifierHandle = securityIdentifierHandle;

        _stdErrorRead = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.None);
        _stdErrorWrite = new AnonymousPipeClientStream(PipeDirection.Out, _stdErrorRead.ClientSafePipeHandle);
        try
        {
            string commandLineArgs = BuildCommandLineArguments(in args);
            PROCESS_INFORMATION processInfo = CreateProcess(processAttributeList, commandLineArgs, _stdErrorWrite.SafePipeHandle);
            _processHandle = new SafeProcessHandle(processInfo.hProcess, ownsHandle: true);
            _threadHandle = new SafeProcessHandle(processInfo.hThread, ownsHandle: true);
            try
            {
                Win32.AssignProcessToJobObject(jobObjHandle, _processHandle);
            }
            catch
            {
                _processHandle.Dispose();
                _threadHandle.Dispose();
                throw;
            }
            finally
            {
                _stdErrorRead.DisposeLocalCopyOfClientHandle();
            }
        }
        catch
        {
            _stdErrorRead.Dispose();
            _stdErrorWrite.Dispose();
            throw;
        }
    }

    private static unsafe PROCESS_INFORMATION CreateProcess(ProcessAttributeListSafeHandle processAttributeList, 
        string commandLineArgs, SafePipeHandle stdErrorWriteHandle)
    {
        const uint CreationFlags = EXTENDED_STARTUPINFO_PRESENT | CREATE_SUSPENDED;

        STARTUPINFOEXW extendedStartupInfo = new()
        { 
            lpAttributeList = (LPPROC_THREAD_ATTRIBUTE_LIST)Helpers.AddRefOrThrow(processAttributeList) 
        };

        try
        {
            extendedStartupInfo.StartupInfo = new STARTUPINFOW()
            {
                cb = (uint)sizeof(STARTUPINFOEXW),
                dwFlags = STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES,
                hStdError = (HANDLE)Helpers.AddRefOrThrow(stdErrorWriteHandle)
            };
        }
        catch
        {
            processAttributeList.DangerousRelease();
            throw;
        }

        try
        {
            return Win32.CreateProcess(in extendedStartupInfo, commandLineArgs, CreationFlags, inheritHandles: true);
        }
        finally
        {
            processAttributeList.DangerousRelease();
            stdErrorWriteHandle.DangerousRelease();
        }
    }

    protected override ProcessExitInfo WaitForProcessExit(out bool success)
    {
        Win32.ResumeThread(_threadHandle);
        Win32.WaitForSingleObject(_processHandle);
        long exitCode = Win32.GetProcessExitCode(_processHandle);
        if (success = exitCode == 0L)
        {
            return default;
        }   
        int processID = Win32.GetProcessID(_processHandle);
        return new ProcessExitInfo(exitCode, processID, _stdErrorRead);
    }

    protected override ProcessException CreateProcessException(ProcessExitInfo processInfo, StringBuilder sb)
    {
        return WindowsProcessException.Create(_executablePath, processInfo.ProcessID, processInfo.ExitCode, processInfo.StdError, sb);
    }

    protected override async Task OnDisposingAsync()
    {
        Task disposing = base.OnDisposingAsync();
        await disposing.ConfigureAwait(false);

        _threadHandle.Dispose();
        _processHandle.Dispose();
        _stdErrorRead.Dispose();
        _stdErrorWrite.Dispose();
        _securityIdentifierHandle.Dispose();
    }
}
