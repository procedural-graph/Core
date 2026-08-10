using System.Runtime.InteropServices;

namespace GameSharp.Collections.Linux;

internal sealed partial class ProcessorInfo : Collections.ProcessorInfo
{
    private const int _SC_LEVEL1_DCACHE_LINESIZE = 190;

    public override int LineCacheSizeInBytes { get; } = (int)GetConfiguration(_SC_LEVEL1_DCACHE_LINESIZE);

    [LibraryImport("libc", EntryPoint = "sysconf")]
    private static partial long GetConfiguration(int name);
}
