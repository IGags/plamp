using System;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Models;

internal class DefaultTypeInfo : ITypeInfo
{
    public Type Type { get; set; }
    public string Alias { get; set; }
    public string Module { get; set; }
}