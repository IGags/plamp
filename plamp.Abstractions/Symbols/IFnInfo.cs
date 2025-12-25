using System;
using System.Collections.Generic;
using System.Reflection;

namespace plamp.Abstractions.Symbols;

public interface IFnInfo : IEquatable<IFnInfo>
{
    public string Name { get; }
    
    public IReadOnlyList<IArgInfo> Arguments { get; }
    
    public ITypeInfo ReturnType { get; }

    public MethodInfo AsFunc();
}