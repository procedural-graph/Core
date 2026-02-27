#if NETFRAMEWORK
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;

namespace ProceduralGraph.Generic;

internal static class CollectionExtensions
{
    public static bool TryPop<T>(this Stack<T> stack, [NotNullWhen(true)] out T? result) where T : notnull
    {
        if (stack.Count > 0)
        {
            result = stack.Pop();
            return true;
        }

        result = default;
        return false;
    }
}
#endif