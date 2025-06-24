using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultConstructorInfo : IConstructorInfo
{
    internal required DefaultTypeInfo Type { get; init; }

    public ITypeInfo EnclosingType => Type;
    
    public required ConstructorInfo ConstructorInfo { get; init; }
}