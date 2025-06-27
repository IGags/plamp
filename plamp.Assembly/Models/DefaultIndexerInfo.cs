using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultIndexerInfo : IIndexerInfo
{
    internal DefaultTypeInfo TypeInfo { get; set; }

    public PropertyInfo IndexerProperty { get; set; }

    public ITypeInfo EnclosingType => TypeInfo;

    public DefaultIndexerInfo(DefaultTypeInfo typeInfo, PropertyInfo indexerProperty)
    {
        TypeInfo = typeInfo;
        IndexerProperty = indexerProperty;
    }
}