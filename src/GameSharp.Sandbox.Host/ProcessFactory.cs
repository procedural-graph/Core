namespace GameSharp.Sandbox;

public abstract class ProcessFactory
{
    public abstract bool TryConfigure<TArgs>(string assemblyPath, ref TArgs args) where TArgs : struct, ICommandLineArguments;
}
