using Microsoft.CodeAnalysis;
using System.Text;

namespace ProceduralGraph.Json.SourceGenerators;

internal sealed class PropertyDataMember(IPropertySymbol symbol) : DataMember
{
    private readonly IPropertySymbol _symbol = symbol;

    public override string Name => _symbol.Name;

    public override string TypeName => _symbol.Type.ToDisplayString();

    public override bool IsWritable => _symbol is { SetMethod.IsInitOnly: false, DeclaredAccessibility: Accessibility.Public };

    public override bool IsReadable => _symbol is { GetMethod: { }, DeclaredAccessibility: Accessibility.Public };

    protected override string ContainingTypeName => _symbol.ContainingType.ToDisplayString();

    public override void PrintUnsafeAccessors(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName)
    {
        if (IsReadableAndWritable(out bool readable, out bool writable))
        {
            return;
        }

        PrintComment(stringBuilder, leadingWhitespace);

        if (!readable)
        {
            PrintUnsafeAccessorAttribute(stringBuilder, leadingWhitespace, GetterPrefix);
            PrintUnsafeAccessorBody(stringBuilder, leadingWhitespace, GetterPrefix);
            stringBuilder.AppendFormat("({0} target)", containingClassName);
            PrintSemicolonLineBreak(stringBuilder);
        }

        if (!writable)
        {
            PrintUnsafeAccessorAttribute(stringBuilder, leadingWhitespace, SetterPrefix);
            PrintUnsafeAccessorBody(stringBuilder, leadingWhitespace, SetterPrefix);
            stringBuilder.AppendFormat("({0} target, {1} value)", containingClassName, TypeName);
            PrintSemicolonLineBreak(stringBuilder);
        }

        stringBuilder.AppendLine();
    }

    protected override void PrintGetPrivate(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors)
    {
        stringBuilder.AppendFormat("Get_{0}(var_{1})", Name, containingClassName);
    }

    protected override void PrintSetPrivate(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors)
    {
        stringBuilder.AppendFormat("Set_{0}(var_{1}, var_{0})", Name, containingClassName);
    }

    private void PrintUnsafeAccessorBody(StringBuilder stringBuilder, int leadingWhitespace, string prefix)
    {
        stringBuilder.AppendIndented("public extern static ", leadingWhitespace);
        stringBuilder.Append(TypeName);
        stringBuilder.Append(' ');
        PrintDelegateName(stringBuilder, prefix);
    }

    private void PrintUnsafeAccessorAttribute(StringBuilder stringBuilder, int leadingWhitespace, string prefix)
    {
        stringBuilder.AppendIndented("[UnsafeAccessor(UnsafeAccessorKind.Method, Name = \"", leadingWhitespace);
        stringBuilder.AppendLowerInvariant(prefix);
        stringBuilder.Append('_');
        stringBuilder.Append(Name);
        stringBuilder.AppendLine("\")]");
    }
}
