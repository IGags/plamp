using System;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultTypeInfo : ITypeInfo
{
    public Type Type { get; set; }

    public string Alias { get; set; }

    public string Module { get; set; }

    public DefaultTypeInfo(string alias, Type type, string module)
    {
        Alias = alias;
        Type = type;
        Module = module;
    }
}