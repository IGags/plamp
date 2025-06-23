using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

public class DefaultPropertyInfo : IPropertyInfo
{
    internal DefaultTypeInfo Type { get; set; }
    public ITypeInfo EnclosingType => Type;
    public PropertyInfo PropertyInfo { get; set; }
    public string Alias { get; set; }
}