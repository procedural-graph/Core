using System;
using System.Diagnostics;

namespace ProceduralGraph;

[Flags]
internal enum TargetPlatform : byte
{
    Unspecified = 0,
    Windows = 1 << 0,
    Linux = 1 << 1,
    MacOS = 1 << 2
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
internal sealed class GuardAttribute : Attribute
{
    public TargetPlatform Platform { get; init; }

    public string? DisposalState { get; init; }

    public string? Implementation { get; init; }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class IndexAttribute : Attribute
{
    public required string Length { get; init; }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class EqualsAttribute : Attribute
{
    public string? ParameterName { get; init; }

    public required string ComparandName { get; init; }

    public string? Message { get; init; }
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class FromAttribute(string getterName) : Attribute
{
    public string Getter { get; } = getterName;
}

[Conditional("BUILD_ONLY")]
[AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false)]
internal sealed class SentinelAttribute : Attribute
{
    public required object? Failure { get; init; }

    public string? Message { get; init; }
}