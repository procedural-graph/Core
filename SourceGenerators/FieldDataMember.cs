using Microsoft.CodeAnalysis;
using System.Text;

namespace ProceduralGraph.Json.SourceGenerators;

internal sealed class FieldDataMember(IFieldSymbol symbol) : DataMember
{
    private readonly IFieldSymbol _symbol = symbol;

    public override string Name => _symbol.Name;

    public override string TypeName => _symbol.Type.ToDisplayString();

    public override bool IsWritable => _symbol is { IsReadOnly: false, DeclaredAccessibility: Accessibility.Public };

    public override bool IsReadable => _symbol.DeclaredAccessibility == Accessibility.Public;

    protected override string ContainingTypeName => _symbol.ContainingType.ToDisplayString();

    public override void PrintUnsafeAccessors(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName)
    {
        if (IsReadable && IsWritable)
        {
            return;
        }

        PrintComment(stringBuilder, leadingWhitespace);

        stringBuilder.AppendLineIndented("[UnsafeAccessor(UnsafeAccessorKind.Field, Name = \"{0}\")]", leadingWhitespace, Name);

        stringBuilder.AppendIndented("public extern static ref ", leadingWhitespace);
        stringBuilder.Append(TypeName);
        stringBuilder.Append(' ');
        PrintDelegateName(stringBuilder, GetterPrefix);
        stringBuilder.Append('(');
        stringBuilder.Append(containingClassName);
        stringBuilder.AppendLine(" target);");

        stringBuilder.AppendLine();
    }

    protected override void PrintGetPrivate(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors)
    {
        PrintDelegateName(stringBuilder, GetterPrefix);
        stringBuilder.AppendFormat("(var_{0})", containingClassName);
    }

    protected override void PrintSetPrivate(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors)
    {
        if (supportsUnsafeAccessors)
        {
            PrintDelegateName(stringBuilder, GetterPrefix);
            stringBuilder.AppendFormat("(var_{1}) = var_{0}", Name, containingClassName);
            return;
        }

        PrintDelegateName(stringBuilder, SetterPrefix);
        stringBuilder.AppendFormat("(var_{1}, var_{0})", Name, containingClassName);
    }
}
