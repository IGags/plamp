using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

public class DefaultMethodInfo : IMethodInfo
{
    internal DefaultTypeInfo Type { get; set; }
    public ITypeInfo EnclosingType => Type;
    public MethodInfo MethodInfo { get; set; }
    public string Alias { get; set; }
}