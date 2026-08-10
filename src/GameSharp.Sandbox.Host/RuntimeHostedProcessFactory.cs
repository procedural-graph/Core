namespace GameSharp.Sandbox;

public abstract class RuntimeHostedProcessFactory(string? runtimeFullName) : ProcessFactory
{
    public abstract string RuntimeDisplayName { get; }

    public string? RuntimeFullName { get; } = runtimeFullName;

    public override bool TryConfigure<TArgs>(string assemblyPath, ref TArgs args)
    {
        if (RuntimeFullName is null)
        {
            return false;
        }

        args.RuntimePath = RuntimeFullName;
        args.AssemblyPath = assemblyPath;

        return true;
    }
}
