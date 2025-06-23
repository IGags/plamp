using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultConstructorInfo : IConstructorInfo
{
    internal DefaultTypeInfo Type { get; set; }

    public ITypeInfo EnclosingType => Type;
    
    public ConstructorInfo ConstructorInfo { get; set; }
}