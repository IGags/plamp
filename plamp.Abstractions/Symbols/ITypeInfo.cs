using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Symbols;

public interface ITypeInfo : IEquatable<ITypeInfo>
{
    public Type AsType();

    public IReadOnlyList<IFieldInfo> Fields { get; }
    
    public string Name { get; }
    
    public bool IsArrayType { get; }

    public ITypeInfo MakeArrayType();

    public ITypeInfo? ElementType();
}