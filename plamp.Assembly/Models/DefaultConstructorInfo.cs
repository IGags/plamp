using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultConstructorInfo : IConstructorInfo
{
    internal DefaultTypeInfo Type { get; set; }

    public ITypeInfo EnclosingType => Type;

    public ConstructorInfo ConstructorInfo { get; set; }

    public DefaultConstructorInfo(DefaultTypeInfo type, ConstructorInfo constructorInfo)
    {
        Type = type;
        ConstructorInfo = constructorInfo;
    }
}