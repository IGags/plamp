using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IIndexerInfo
{
    public PropertyInfo IndexerProperty { get; }
    
    public ITypeInfo EnclosingType { get; }
}