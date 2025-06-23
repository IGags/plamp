using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IPropertyInfo
{
    public ITypeInfo EnclosingType { get; }
    
    public PropertyInfo PropertyInfo { get; }
    
    public string Alias { get; }
}