using System.Runtime.CompilerServices;
using System.Text;

namespace GameSharp.Sandbox;

internal static class StringBuilderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendPath(this StringBuilder sb, string path)
    {
        sb.Append('"');
        sb.Append(path);
        sb.Append('"');
    }

#if !NETFRAMEWORK
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendPath(this StringBuilder sb, System.ReadOnlySpan<char> path)
    {
        sb.Append('"');
        sb.Append(path);
        sb.Append('"');
    }
#endif
}
