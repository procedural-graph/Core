namespace GameSharp.Sandbox.Dotnet;

internal class DotnetProcessFactoryProvider : RuntimeHostedProcessFactoryProvider
{
    public override string SearchPattern
    {
        get
        {
            if (Platform.IsWindows())
            {
                return "dotnet.exe";
            }
            else
            {
                return "dotnet";
            }
        }
    }

    public override string AssemblyFileExtension => ".dll";

    protected override RuntimeHostedProcessFactory Create(string? runtimeFullName)
    {
        return new DotnetProcessFactory(runtimeFullName);
    }
}
