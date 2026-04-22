using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProceduralGraph.Json.SourceGenerators;

internal abstract class DataMember
{
    protected const string GetterPrefix = "Get";
    protected const string SetterPrefix = "Set";

    public abstract string Name { get; }

    public abstract string TypeName { get; }

    public abstract bool IsWritable { get; }

    public abstract bool IsReadable { get; }

    protected abstract string ContainingTypeName { get; }

    public abstract void PrintUnsafeAccessors(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrintDelegateDefinitions(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName)
    {
        if (IsReadableAndWritable(out bool readable, out bool writable))
        {
            return;
        }

        PrintComment(stringBuilder, leadingWhitespace);

        if (!readable)
        {
            PrintDelegateDefinition(stringBuilder, leadingWhitespace, containingClassName, GetterPrefix, "Func");
        }

        if (!writable)
        {
            PrintDelegateDefinition(stringBuilder, leadingWhitespace, containingClassName, SetterPrefix, "Action");
        }

        stringBuilder.AppendLine();
    }

    public void PrintDelegateInitializers(StringBuilder stringBuilder, int leadingWhitespace)
    {
        if (IsReadableAndWritable(out bool readable, out bool writable))
        {
            return;
        }
        stringBuilder.AppendIndented('(', leadingWhitespace);
        if (readable)
        {
            stringBuilder.Append('_');
        }
        else
        {
            PrintDelegateName(stringBuilder, GetterPrefix);
        }
        stringBuilder.Append(", ");
        if (writable)
        {
            stringBuilder.Append('_');
        }
        else
        {
            PrintDelegateName(stringBuilder, SetterPrefix);
        }     
        stringBuilder.AppendFormat(") = CreateAccessors<{0}>(\"{1}\");", TypeName, Name);
        stringBuilder.AppendLine();
    }

    public void PrintJsonWriteLogic(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName, bool supportsUnsafeAccessors)
    {
        stringBuilder.AppendLineIndented("writer.WritePropertyName(\"{0}\"u8);", leadingWhitespace, Name);
        PrintDeclareAndAssignLocal(stringBuilder, leadingWhitespace);
        if (IsReadable)
        {
            PrintGetExposed(stringBuilder, containingClassName, supportsUnsafeAccessors);
        }
        else
        {
            PrintGetPrivate(stringBuilder, containingClassName, supportsUnsafeAccessors);
        }
        PrintSemicolonLineBreak(stringBuilder);
        stringBuilder.AppendLineIndented("JsonSerializer.Serialize(writer, var_{0}, options);", leadingWhitespace, Name);
    }

    public void PrintJsonReadLogic(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName, bool supportsUnsafeAccessors)
    {        
        stringBuilder.AppendLineIndented("reader.Read();", leadingWhitespace);
        PrintDeclareAndAssignLocal(stringBuilder, leadingWhitespace);
        stringBuilder.AppendFormat("JsonSerializer.Deserialize<{0}>(ref reader, options)", TypeName);
        PrintSemicolonLineBreak(stringBuilder);
        stringBuilder.AppendIndented(leadingWhitespace);
        if (IsWritable)
        {
            PrintSetExposed(stringBuilder, containingClassName, supportsUnsafeAccessors);
        }
        else
        {
            PrintSetPrivate(stringBuilder, containingClassName, supportsUnsafeAccessors);
        }   
        PrintSemicolonLineBreak(stringBuilder);
    }

    protected virtual void PrintGetExposed(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors)
    {
        stringBuilder.Append("var_");
        stringBuilder.Append(containingClassName);
        stringBuilder.Append('.');
        stringBuilder.Append(Name);
    }

    protected abstract void PrintGetPrivate(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors);

    protected virtual void PrintSetExposed(StringBuilder stringBuilder, string containingClassName, bool supportsUnsafeAccessors)
    {
        stringBuilder.Append("var_");
        stringBuilder.Append(containingClassName);
        stringBuilder.Append('.');
        stringBuilder.Append(Name);
        stringBuilder.Append(" = var_");
        stringBuilder.Append(Name);
    }

    protected abstract void PrintSetPrivate(StringBuilder stringBuilder, string containingClassInstanceName, bool supportsUnsafeAccessors);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PrintComment(StringBuilder stringBuilder, int leadingWhitespace)
    {
        stringBuilder.AppendLineIndented("// {0}.{1}", leadingWhitespace, ContainingTypeName, Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool IsReadableAndWritable(out bool readable, out bool writable)
    {
        readable = IsReadable;
        writable = IsWritable;
        return readable && writable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void PrintDelegateName(StringBuilder stringBuilder, string prefix)
    {
        stringBuilder.Append(prefix);
        stringBuilder.Append('_');
        stringBuilder.Append(Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static void PrintSemicolonLineBreak(StringBuilder stringBuilder)
    {
        stringBuilder.Append(';');
        stringBuilder.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrintDeclareAndAssignLocal(StringBuilder stringBuilder, int leadingWhitespace)
    {
        stringBuilder.AppendIndented(TypeName, leadingWhitespace);
        stringBuilder.Append(" var_");
        stringBuilder.Append(Name);
        stringBuilder.Append(" = ");
    }

    private void PrintDelegateDefinition(StringBuilder stringBuilder, int leadingWhitespace, string containingClassName, string prefix, string type)
    {
        stringBuilder.AppendIndented("private static ", leadingWhitespace);
        stringBuilder.Append(type);
        stringBuilder.Append('<');
        stringBuilder.Append(containingClassName);
        stringBuilder.Append(", ");
        stringBuilder.Append(TypeName);
        stringBuilder.Append("> ");
        PrintDelegateName(stringBuilder, prefix);
        stringBuilder.AppendLine(" { get; }");
    }
}
