using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IConstructorInfo
{
    public ITypeInfo EnclosingType { get; }
    
    public ConstructorInfo ConstructorInfo { get; }
}