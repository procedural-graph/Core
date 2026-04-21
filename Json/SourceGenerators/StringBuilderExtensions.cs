using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProceduralGraph.Json.SourceGenerators;

internal static class StringBuilderExtensions
{
    private const int WhitespacePerIndentation = 4;

    public static void BeginScope(this StringBuilder stringBuilder, ref int leadingWhitespace)
    {
        AppendIndented(stringBuilder, leadingWhitespace);
        leadingWhitespace += WhitespacePerIndentation;
        stringBuilder.AppendLine("{");
    }

    public static void EndScope(this StringBuilder stringBuilder, ref int leadingWhitespace)
    {
        leadingWhitespace -= WhitespacePerIndentation;
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.AppendLine("}");
    }

    public static void EndScopeWithSemicolon(this StringBuilder stringBuilder, ref int leadingWhitespace)
    {
        leadingWhitespace -= WhitespacePerIndentation;
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.AppendLine("};");
    }

    public static void AppendIndented(this StringBuilder stringBuilder, string text, int leadingWhitespace)
    {
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.Append(text);
    }

    public static void AppendIndented(this StringBuilder stringBuilder, char value, int leadingWhitespace)
    {
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.Append(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendLowerInvariant(this StringBuilder stringBuilder, ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            stringBuilder.Append(char.ToLowerInvariant(c));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendUpperInvariant(this StringBuilder stringBuilder, ReadOnlySpan<char> text)
    {
        foreach (char c in text)
        {
            stringBuilder.Append(char.ToUpperInvariant(c));
        }
    }

    public static void AppendLineIndented(this StringBuilder stringBuilder, string line, int leadingWhitespace)
    {
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.AppendLine(line);
    }

    public static void AppendLineIndented(this StringBuilder stringBuilder, string format, int leadingWhitespace, object arg0)
    {
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.AppendFormat(format, arg0);
        stringBuilder.AppendLine();
    }

    public static void AppendLineIndented(this StringBuilder stringBuilder, string format, int leadingWhitespace, object arg0, object arg1)
    {
        AppendIndented(stringBuilder, leadingWhitespace);
        stringBuilder.AppendFormat(format, arg0, arg1);
        stringBuilder.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendIndented(this StringBuilder stringBuilder, int leadingWhitespace)
    {
        for (int i = 0; i < leadingWhitespace; i++)
        {
            stringBuilder.Append(' ');
        }
    }
}
