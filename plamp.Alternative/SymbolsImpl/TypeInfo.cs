using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsImpl;

public class TypeInfo(Type type) : ITypeInfo
{
    public Type AsType() => type;

    public IReadOnlyList<IFieldInfo> Fields { get; } = type.GetFields().Select(x => new FldInfo(x)).ToList();

    public string Name => type.Name;
}