using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IMethodInfo
{
    public ITypeInfo EnclosingType { get; }
    
    public MethodInfo MethodInfo { get; }
    
    public string Alias { get; }
}