namespace GameSharp.Sandbox.Java;

internal class JavaProcessFactoryProvider : RuntimeHostedProcessFactoryProvider
{
    public override string SearchPattern
    {
        get
        {
            if (Platform.IsWindows())
            {
                return "java.exe";
            }
            else
            {
                return "java";
            }
        }
    }

    public override string AssemblyFileExtension => ".dll";

    protected override RuntimeHostedProcessFactory Create(string? runtimeFullName)
    {
        return new JavaProcessFactory(runtimeFullName);
    }
}
