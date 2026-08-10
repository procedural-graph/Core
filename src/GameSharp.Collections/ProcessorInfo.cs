namespace GameSharp.Collections;

internal class ProcessorInfo
{
    public static ProcessorInfo Default { get; }

    static ProcessorInfo()
    {
        if (OperatingSystem.IsWindows())
        {
            Default = new Windows.ProcessorInfo();
        }
        else if (OperatingSystem.IsMacOS())
        {
            Default = new Mac.ProcessorInfo();
        }
        else if (OperatingSystem.IsLinux())
        {
            Default = new Linux.ProcessorInfo();
        }
        else
        {
            Default = new ProcessorInfo();
        }
    }

    public virtual int LineCacheSizeInBytes => 64;
}
