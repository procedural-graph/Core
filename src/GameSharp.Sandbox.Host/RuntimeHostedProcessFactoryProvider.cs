using System.IO;
using System.Runtime.CompilerServices;

namespace GameSharp.Sandbox;

public abstract class RuntimeHostedProcessFactoryProvider
{
    public abstract string SearchPattern { get; }

    public abstract string AssemblyFileExtension { get; }

    internal RuntimeHostedProcessFactory Create(ref string start, ref string end)
    {
        string searchPattern = SearchPattern;

        for (ref string current = ref start; Unsafe.IsAddressLessThan(ref current, ref end); current = ref Unsafe.Add(ref current, 1))
        {
            if (!Directory.Exists(current))
            {
                ShrinkRange(ref end, ref current);
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(current, searchPattern, SearchOption.TopDirectoryOnly))
            {
                RuntimeHostedProcessFactory factory = Create(path);
                ShrinkRange(ref end, ref current);
                return factory;
            }
        }

        return Create(null);
    }

    protected abstract RuntimeHostedProcessFactory Create(string? runtimeFullName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ShrinkRange(ref string end, ref string current)
    {
        end = ref Unsafe.Subtract(ref end, 1);
        if (!Unsafe.AreSame(ref current, ref end))
        {
            (current, end) = (end, null!);
        }
    }
}
