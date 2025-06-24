using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultPropertyInfo : IPropertyInfo
{
    internal required DefaultTypeInfo Type { get; init; }
    public ITypeInfo EnclosingType => Type;
    public required PropertyInfo PropertyInfo { get; init; }
    public string? Alias { get; set; }
}