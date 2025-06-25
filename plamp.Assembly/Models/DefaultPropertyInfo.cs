using System.Reflection;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultPropertyInfo : IPropertyInfo
{
    internal DefaultTypeInfo Type { get; set; }

    public ITypeInfo EnclosingType => Type;

    public PropertyInfo PropertyInfo { get; set; }

    public string Alias { get; set; }

    public DefaultPropertyInfo(string alias, DefaultTypeInfo type, PropertyInfo propertyInfo)
    {
        Alias = alias;
        Type = type;
        PropertyInfo = propertyInfo;
    }
}