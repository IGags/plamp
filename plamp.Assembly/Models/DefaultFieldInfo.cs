using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultFieldInfo : IFieldInfo
{
    internal required DefaultTypeInfo Type { get; init; }
    public ITypeInfo EnclosingType => Type;
    public required FieldInfo FieldInfo { get; init; }
    public string? Alias { get; set; }
}