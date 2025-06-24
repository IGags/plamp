using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultIndexerInfo : IIndexerInfo
{
    internal required DefaultTypeInfo TypeInfo { get; init; }
    
    public required PropertyInfo IndexerProperty { get; init; }

    public ITypeInfo EnclosingType => TypeInfo;
}