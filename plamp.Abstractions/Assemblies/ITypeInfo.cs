using System;

namespace plamp.Abstractions.Assemblies;

public interface ITypeInfo
{
    public Type Type { get; }

    public string Alias { get; }
    
    public string Module { get; }
}