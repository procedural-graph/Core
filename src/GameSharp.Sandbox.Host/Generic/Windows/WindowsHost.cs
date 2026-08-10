using GameSharp.Sandbox.Windows;
using Microsoft.Win32.SafeHandles;
using StreamJsonRpc;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO.Pipes;
using Windows.Win32.Security;
using Windows.Win32.System.JobObjects;

namespace GameSharp.Sandbox.Generic.Windows;

internal sealed class WindowsHost : Host<CommandLineArguments>
{
    private readonly SafeProcessHandle _jobObjHandle;
    private readonly AppContainerProfile _profile;

    public WindowsHost(IJsonRpcFactory factory, FrozenDictionary<string, RuntimeHostedProcessFactory> runtimeProcessFactories,
        ImmutableArray<ProcessFactory> processFactories) : base(factory, runtimeProcessFactories, processFactories)
    {
        _profile = new AppContainerProfile("GameSharp Shared Sandbox", "Shared restricted environment for untrusted workers.");
        _jobObjHandle = Win32.CreateJobObject();
        try
        {
            JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = new()
            {
                BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
                {
                    LimitFlags = JOB_OBJECT_LIMIT.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                }
            };
            Win32.SetJobObjectInformation(_jobObjHandle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, limitInfo);
        }
        catch
        {
            _profile.Dispose();
            _jobObjHandle.Dispose();
            throw;
        }
    }

    protected override Guest Launch(JsonRpc jsonRpc, AnonymousPipeServerStream outboundPipe, AnonymousPipeServerStream inboundPipe, ref CommandLineArguments args)
    {
        SafeSecurityIdentifierHandle sid = _profile.GetSecurityIdentifier();
        try
        {
            SECURITY_CAPABILITIES secCaps = new()
            {
                AppContainerSid = (PSID)Helpers.AddRefOrThrow(sid),
                Capabilities = null,
                CapabilityCount = 0,
                Reserved = 0
            };
            try
            {
                using ProcessAttributeListSafeHandle processAttributeList = Win32.CreateProcessAttributeList(dwAttributeCount: 1u);
                Win32.UpdateProcessThreadAttribute(processAttributeList, 0x00020009, in secCaps);
                return new WindowsGuest(outboundPipe, inboundPipe, jsonRpc, _jobObjHandle, sid, processAttributeList, in args, this);
            }
            finally
            {
                sid.DangerousRelease();
            }
        }
        catch
        {
            sid.Dispose();
            throw;
        }
    }

    protected override void OnDisposing()
    {
        base.OnDisposing();
        _jobObjHandle.Dispose();
        _profile.Dispose();
    }
}
