using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsImpl;

public class TypeInfo(Type type) : ITypeInfo
{
    private readonly Type _type = type;
    
    public Type AsType() => _type;

    public IReadOnlyList<IFieldInfo> Fields { get; } = type.GetFields().Select(x => new FldInfo(x)).ToList();

    public string Name => _type.Name;

    public bool IsArrayType => _type.IsArray;
    
    public ITypeInfo MakeArrayType()
    {
        return new TypeInfo(_type.MakeArrayType());
    }

    public ITypeInfo? ElementType()
    {
        return !_type.IsArray ? null : new TypeInfo(_type.GetElementType()!);
    }

    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeInfo typ) return false;
        return typ._type == _type;
    }
}