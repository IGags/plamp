using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Symbols;

public interface ITypeInfo
{
    public Type AsType();

    public IReadOnlyList<IFieldInfo> Fields { get; }
    
    public string Name { get; }
}