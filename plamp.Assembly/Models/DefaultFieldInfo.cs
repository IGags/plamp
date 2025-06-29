using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultFieldInfo : IFieldInfo
{
    internal DefaultTypeInfo Type { get; set; }

    public ITypeInfo EnclosingType => Type;

    public FieldInfo FieldInfo { get; set; }

    public string Alias { get; set; }

    public DefaultFieldInfo(DefaultTypeInfo type, string alias, FieldInfo fieldInfo)
    {
        Type = type;
        Alias = alias;
        FieldInfo = fieldInfo;
    }
}