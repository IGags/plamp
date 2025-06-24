using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultMethodInfo : IMethodInfo
{
    internal required DefaultTypeInfo Type { get; init; }
    public ITypeInfo EnclosingType => Type;
    public required MethodInfo MethodInfo { get; init; }
    public string? Alias { get; set; }
}