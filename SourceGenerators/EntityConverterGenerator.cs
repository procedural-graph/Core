using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ProceduralGraph.Json.SourceGenerators;

[Generator]
public class EntityConverterGenerator : IIncrementalGenerator
{
    private const string SerializeAttribute = "SerializeAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<(bool SupportsAccessors, bool AllowUnsafe)> compilationData = context.CompilationProvider
            .Select(static (compilation, _) =>
            {
                bool supportsAccessors = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.UnsafeAccessorAttribute") != null;

                bool allowUnsafe = false;
                if (compilation.Options is Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions csharpOptions)
                {
                    allowUnsafe = csharpOptions.AllowUnsafe;
                }

                return (supportsAccessors, allowUnsafe);
            });

        IncrementalValuesProvider<INamedTypeSymbol> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { BaseList.Types.Count: > 0 },
                transform: static (ctx, _) => GetClassIfInheritsEntity(ctx))
            .Where(static m => m is not null)!;

        var combined = classDeclarations.Combine(compilationData);

        context.RegisterSourceOutput(combined, static (spc, source) =>
            Execute(spc, source.Left, source.Right.SupportsAccessors, source.Right.AllowUnsafe));

        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol>> allSymbols = classDeclarations.Collect()!;
        context.RegisterSourceOutput(allSymbols, static (spc, symbols) => ExecuteExtensionMethod(spc, symbols));
    }

    private static INamedTypeSymbol? GetClassIfInheritsEntity(GeneratorSyntaxContext context)
    {
        if (context.SemanticModel.GetDeclaredSymbol((ClassDeclarationSyntax)context.Node) is INamedTypeSymbol { IsAbstract: false } symbol)
        {
            INamedTypeSymbol? currentSymbol = symbol;
            while (currentSymbol is { } and not { SpecialType: SpecialType.System_Object })
            {
                if (currentSymbol is { Name: "Entity", ContainingNamespace.Name: "ProceduralGraph" })
                {
                    return symbol;
                }

                currentSymbol = currentSymbol.BaseType;
            }
        }

        return null;
    }

    private static IMethodSymbol? GetBestConstructor(INamedTypeSymbol classSymbol)
    {
        IMethodSymbol? result = null;
        int maxParameterCount = int.MinValue;

        foreach (IMethodSymbol constructor in classSymbol.Constructors)
        {
            if (constructor is { IsStatic: true } or { DeclaredAccessibility: not Accessibility.Public })
            {
                continue;
            }

            int parameterCount = constructor.Parameters.Length;
            if (parameterCount > maxParameterCount)
            {
                result = constructor;
                maxParameterCount = parameterCount;
            }
        }

        return result;
    }

    private static ImmutableArray<DataMember> CollectMembers(INamedTypeSymbol classSymbol, out int maxNameLength)
    {
        maxNameLength = int.MinValue;
        ImmutableArray<DataMember>.Builder members = ImmutableArray.CreateBuilder<DataMember>();
        INamedTypeSymbol? currentSymbol = classSymbol;
        do
        {
            foreach (ISymbol member in currentSymbol.GetMembers())
            {
                if (member is IFieldSymbol { IsStatic: false, IsConst: false } field && HasSerializeAttribute(field))
                {
                    members.Add(new FieldDataMember(field));
                    maxNameLength = Math.Max(maxNameLength, field.Name.Length);
                }
                else if (member is IPropertySymbol { IsStatic: false } property && HasSerializeAttribute(property))
                {
                    members.Add(new PropertyDataMember(property));
                    maxNameLength = Math.Max(maxNameLength, property.Name.Length);
                }
            }

            currentSymbol = currentSymbol.BaseType;
        }
        while (currentSymbol is { } and not { SpecialType: SpecialType.System_Object });
        return members.ToImmutable();
    }

    private static bool HasSerializeAttribute(ISymbol member)
    {
        foreach (AttributeData attribute in member.GetAttributes())
        {
            if (attribute.AttributeClass is { Name: SerializeAttribute })
            {
                return true;
            }
        }

        return false;
    }

    private static unsafe void Execute(SourceProductionContext context, INamedTypeSymbol classSymbol, bool supportsUnsafeAccessors, bool allowUnsafe)
    {
        if (GetBestConstructor(classSymbol) is not { } constructor)
        {
            return;
        }

        string className = classSymbol.Name;
        string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

        ImmutableArray<DataMember> members = CollectMembers(classSymbol, out int maxNameLength);
        int setterSize;
        bool useAggressiveOptimization = false;
        if (supportsUnsafeAccessors && allowUnsafe)
        {
            setterSize = sizeof(nint) + Encoding.UTF8.GetMaxByteCount(maxNameLength) + sizeof(int);
            useAggressiveOptimization = (setterSize * members.Length) <= 1024;
        }
        else
        {
            setterSize = 0;
        }

        StringBuilder sb = new();
        int leadingWhitespace = 0;

        // Using directives
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine("using ProceduralGraph.Serialization.Json;");
        if (useAggressiveOptimization)
        {
            sb.AppendLine("using System.Runtime.InteropServices;");
        }
        if (supportsUnsafeAccessors)
        {
            sb.AppendLine("using System.Runtime.CompilerServices;");
        }
        else
        {
            sb.AppendLine("using System.Reflection;");
        }
        sb.AppendLine();

        // Namespace declaration
        sb.AppendLineIndented("namespace {0}", leadingWhitespace, namespaceName);
        sb.BeginScope(ref leadingWhitespace);

        // Class declaration
        sb.AppendLineIndented("internal sealed class {0}JsonConverter : DefaultJsonConverter<{0}>", leadingWhitespace, className);
        sb.BeginScope(ref leadingWhitespace);

        if (useAggressiveOptimization)
        {       
            sb.AppendIndented("[StructLayout(LayoutKind.Sequential, Size = ", leadingWhitespace);
            sb.Append(setterSize);
            sb.AppendLine("), SkipLocalsInit]");
            sb.AppendLineIndented("private unsafe struct MemberDeserializer : IMemberDeserializer", leadingWhitespace);
            sb.BeginScope(ref leadingWhitespace);
            sb.AppendLineIndented("public delegate*<ref Utf8JsonReader, {0}, JsonSerializerOptions, void> Method {{ get; }}", 
                leadingWhitespace, className);
            sb.AppendLineIndented("private readonly int _length;", leadingWhitespace);
            sb.AppendLineIndented("private byte _name;", leadingWhitespace);
            sb.AppendLineIndented("public ReadOnlySpan<byte> Name => MemoryMarshal.CreateReadOnlySpan(ref _name, _length);", leadingWhitespace);
            sb.AppendLine();
            sb.AppendLineIndented("public MemberDeserializer(delegate*<ref Utf8JsonReader, {0}, JsonSerializerOptions, void> method, ReadOnlySpan<byte> name)", 
                leadingWhitespace, className);
            sb.BeginScope(ref leadingWhitespace);
            sb.AppendLineIndented("Method = method;", leadingWhitespace);
            sb.AppendLineIndented("_length = name.Length;", leadingWhitespace);
            sb.AppendLineIndented("Unsafe.CopyBlockUnaligned(ref _name, ref MemoryMarshal.GetReference(name), (uint)_length);", leadingWhitespace);
            sb.EndScope(ref leadingWhitespace);
            sb.EndScope(ref leadingWhitespace);
            sb.AppendLine();
        }

        // Member declarations
        sb.AppendLineIndented("private readonly Repository _services;", leadingWhitespace);
        sb.AppendLine();

        // Accessor definitions
        if (supportsUnsafeAccessors)
        {
            foreach (DataMember member in members)
            {
                member.PrintUnsafeAccessors(sb, leadingWhitespace, className);
            }
        }
        else
        {
            foreach (DataMember member in members)
            {
                member.PrintDelegateDefinitions(sb, leadingWhitespace, className);
            }

            // Static constructor
            sb.AppendLineIndented("static {0}JsonConverter()", leadingWhitespace, className);
            sb.BeginScope(ref leadingWhitespace);
            foreach (DataMember member in members)
            {
                member.PrintDelegateInitializers(sb, leadingWhitespace);
            }
            sb.EndScope(ref leadingWhitespace);
            sb.AppendLine();
        }

        // Instance Constructor
        sb.AppendLineIndented("public {0}JsonConverter(Repository services)", leadingWhitespace, className);
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("_services = services;", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();

        // Write method
        sb.AppendLineIndented("public override void Write(Utf8JsonWriter writer, {0} var_{0}, JsonSerializerOptions options)", leadingWhitespace, className);
        sb.BeginScope(ref leadingWhitespace);
        if (!members.IsEmpty)
        {
            sb.AppendLineIndented("writer.WriteStartObject();", leadingWhitespace);
            sb.AppendLine();
            foreach (DataMember member in members)
            {
                member.PrintJsonWriteLogic(sb, leadingWhitespace, className, supportsUnsafeAccessors);
                sb.AppendLine();
            }
            sb.AppendLineIndented("writer.WriteEndObject();", leadingWhitespace);
        }
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();

        // Read method
        sb.AppendIndented("public ", leadingWhitespace);
        if (useAggressiveOptimization)
        {
            sb.Append("unsafe ");
        }
        sb.AppendFormat("override {0}? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)", className);
        sb.AppendLine();
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("ThrowIfUnexpectedToken(ref reader, JsonTokenType.StartObject);", leadingWhitespace);
        sb.AppendLine();
        ImmutableArray<IParameterSymbol> ctorParams = constructor.Parameters;
        foreach (IParameterSymbol ctorParam in ctorParams)
        {
            sb.AppendLineIndented("{0} {1} = _services.GetOne<{0}>();", leadingWhitespace, ctorParam.Type.ToDisplayString(), ctorParam.Name);
        }
        sb.AppendIndented(className, leadingWhitespace);
        sb.Append(" var_");
        sb.Append(className);
        sb.Append(" = new ");
        sb.Append(className);
        sb.Append('(');
        if (!ctorParams.IsEmpty)
        {
            sb.Append(ctorParams[0].Name);
            for (int i = 1; i < ctorParams.Length; i++)
            {
                sb.Append(", ");
                sb.Append(ctorParams[i].Name);
            }
        }
        sb.AppendLine(");");
        sb.AppendLine();
        if (useAggressiveOptimization)
        {
            sb.AppendLineIndented("Span<MemberDeserializer> deserializers = stackalloc MemberDeserializer[]", leadingWhitespace);
            sb.BeginScope(ref leadingWhitespace);
            const string SetterInitializer = "new MemberDeserializer(&Deserialize_{0}, \"{0}\"u8)";
            for (int i = members.Length - 1; i > 0; i--)
            {
                sb.AppendIndented(leadingWhitespace);
                sb.AppendFormat(SetterInitializer, members[i].Name);
                sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLineIndented(SetterInitializer, leadingWhitespace, members[0].Name);
            sb.EndScopeWithSemicolon(ref leadingWhitespace);
            sb.AppendLine();
            sb.AppendLineIndented("while (!deserializers.IsEmpty && reader.Read() && reader.TokenType != JsonTokenType.EndObject)", leadingWhitespace);
            sb.BeginScope(ref leadingWhitespace);
            sb.AppendLineIndented("if (DeserializeTruncate(ref reader, ref deserializers, var_{0}, options))", leadingWhitespace, className);
            sb.BeginScope(ref leadingWhitespace);
            sb.AppendLineIndented("continue;", leadingWhitespace);
            sb.EndScope(ref leadingWhitespace);
            sb.AppendLine();
            sb.AppendLineIndented("HandleUnmappedMember(ref reader, options);", leadingWhitespace);
            sb.EndScope(ref leadingWhitespace);
        }
        else
        {
            sb.AppendLineIndented("while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)", leadingWhitespace);
            sb.BeginScope(ref leadingWhitespace);
            sb.AppendIndented(leadingWhitespace);
            foreach (DataMember member in members)
            {
                sb.Append("if (reader.ValueTextEquals(\"");
                sb.Append(member.Name);
                sb.AppendLine("\"u8))");
                sb.BeginScope(ref leadingWhitespace);
                member.PrintJsonReadLogic(sb, leadingWhitespace, className, supportsUnsafeAccessors);
                sb.EndScope(ref leadingWhitespace);
                sb.AppendIndented("else ", leadingWhitespace);
            }
            sb.AppendLine();
            sb.BeginScope(ref leadingWhitespace);
            sb.AppendLineIndented("HandleUnmappedMember(ref reader, options);", leadingWhitespace);
            sb.EndScope(ref leadingWhitespace);
            sb.EndScope(ref leadingWhitespace);
        }
        sb.AppendLine();
        sb.AppendLineIndented("return var_{0};", leadingWhitespace, className);
        sb.EndScope(ref leadingWhitespace);

        if (useAggressiveOptimization)
        {
            foreach (DataMember member in members)
            {
                sb.AppendLine();
                sb.AppendLineIndented("private static void Deserialize_{0}(ref Utf8JsonReader reader, {1} var_{1}, JsonSerializerOptions options)",
                    leadingWhitespace, member.Name, className);
                sb.BeginScope(ref leadingWhitespace);
                member.PrintJsonReadLogic(sb, leadingWhitespace, className, supportsUnsafeAccessors);
                sb.EndScope(ref leadingWhitespace);
            }
        }

        sb.EndScope(ref leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);

        context.AddSource($"{className}JsonConverter.gen.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static void ExecuteExtensionMethod(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> classSymbols)
    {
        if (classSymbols.IsDefaultOrEmpty)
        {
            return;
        }

        StringBuilder sb = new();
        int leadingWhitespace = 0;

        // Using directives
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine("using System.Text.Json.Serialization.Metadata;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();

        // Namespace declaration
        sb.AppendLineIndented("namespace ProceduralGraph.Json", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);

        // Public static class declaration
        sb.AppendLineIndented("public static partial class JsonSerializerOptionsExtensions", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);

        sb.AppendLineIndented("/// <summary>", leadingWhitespace);
        sb.AppendLineIndented("/// Adds all <see cref=\"Entity\"/> converters to the <see cref=\"JsonSerializerOptions.Converters\"/> collection.",
            leadingWhitespace);
        sb.AppendLineIndented("/// </summary>", leadingWhitespace);
        sb.AppendLineIndented("/// <param name=\"options\">The <see cref=\"JsonSerializerOptions\"/> to add the converters to.</param>", leadingWhitespace);
        sb.AppendLineIndented("/// <param name=\"services\">The repository to resolve services from.</param>", leadingWhitespace);
        sb.AppendLineIndented("/// <returns>The <see cref=\"JsonSerializerOptions\"/> with the converters added.</returns>", leadingWhitespace);
        sb.AppendLineIndented("public static JsonSerializerOptions AddEntityConverters(this JsonSerializerOptions options, Repository services)",
            leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);

        // Guard clauses
        sb.AppendLineIndented("if (options is null)", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("throw new ArgumentNullException(nameof(options));", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();
        sb.AppendLineIndented("if (services is null)", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("throw new ArgumentNullException(nameof(services));", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();

        INamedTypeSymbol[] uniqueSymbols = [.. classSymbols.Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)];

        // Add converters
        sb.AppendLineIndented("IList<JsonConverter> converters = options.Converters;", leadingWhitespace);
        sb.AppendLine();
        foreach (INamedTypeSymbol symbol in uniqueSymbols)
        {
            string namespaceName = symbol.ContainingNamespace.ToDisplayString();
            string className = symbol.Name;
            sb.AppendLineIndented("converters.Add(new {0}.{1}JsonConverter(services));", leadingWhitespace, namespaceName, className);
        }
        sb.AppendLine();

        // Configure polymorphic serialization for Entity
        sb.AppendLineIndented("if (options.TypeInfoResolver is { } resolver)", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("options.TypeInfoResolver = resolver.WithAddedModifier(ConfigureEntityPolymorphism);", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLineIndented("else", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("options.TypeInfoResolver = CreateJsonTypeInfoResolver(ConfigureEntityPolymorphism);", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();

        sb.AppendLineIndented("return options;", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();

        // Private helper method declaration
        sb.AppendLineIndented("private static void ConfigureEntityPolymorphism(JsonTypeInfo typeInfo)", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);

        // Guard clauses
        sb.AppendLineIndented("if (typeInfo.Type != typeof(Entity))", leadingWhitespace);
        sb.BeginScope(ref leadingWhitespace);
        sb.AppendLineIndented("return;", leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);
        sb.AppendLine();

        // Configure polymorphism options
        sb.AppendLineIndented("typeInfo.PolymorphismOptions ??= CreateDefaultPolymorphismOptions();", leadingWhitespace);
        sb.AppendLineIndented("IList<JsonDerivedType> derivedTypes = typeInfo.PolymorphismOptions.DerivedTypes;", leadingWhitespace);
        sb.AppendLine();

        // Add derived types
        foreach (INamedTypeSymbol symbol in uniqueSymbols)
        {
            sb.AppendIndented("derivedTypes.Add(new JsonDerivedType(typeof(", leadingWhitespace);
            sb.Append(symbol.ContainingNamespace.ToDisplayString());
            sb.Append('.');
            sb.Append(symbol.Name);
            sb.Append("), \"");
            sb.Append(symbol.Name);
            sb.AppendLine("\"));");
        }
        sb.EndScope(ref leadingWhitespace);

        sb.EndScope(ref leadingWhitespace);
        sb.EndScope(ref leadingWhitespace);

        context.AddSource("ProceduralGraphSerializationExtensions.gen.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}