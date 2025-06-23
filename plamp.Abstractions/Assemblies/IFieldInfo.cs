using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IFieldInfo
{
    public ITypeInfo EnclosingType { get; }
    
    public FieldInfo FieldInfo { get; }
    
    public string Alias { get; }
}