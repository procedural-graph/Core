using System.Text;

namespace GameSharp.Sandbox;

public interface ICommandLineArguments
{
    string? RuntimePath { get; set; }

    string AssemblyPath { get; set; }

    string OutboundPipeHandle { get; set; }

    string InboundPipeHandle { get; set; }

    string ToString(StringBuilder sb);
}
