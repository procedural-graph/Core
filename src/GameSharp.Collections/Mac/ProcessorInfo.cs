using System.Runtime.InteropServices;

namespace GameSharp.Collections.Mac;

internal sealed partial class ProcessorInfo : Collections.ProcessorInfo
{
    public override int LineCacheSizeInBytes { get; }

    public ProcessorInfo()
    {
        nint sizeOfLineSize = nint.Size;
        SystemInformationByName("hw.cachelinesize", out nint lineSize, ref sizeOfLineSize, nint.Zero, nint.Zero);
        LineCacheSizeInBytes = lineSize.ToInt32();
    }

    [LibraryImport("libc", EntryPoint = "sysctlbyname", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int SystemInformationByName(string name, out nint oldp, ref nint oldlenp, nint newp, nint newlen);
}
