using System;
using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class ArrayTypeBuilder(ITypeInfo elementType) : ITypeInfo
{
    public IReadOnlyList<IFieldInfo> Fields => [];

    public string Name => elementType.Name;

    public bool IsArrayType => true;

    public bool IsGenericType => false;

    public bool IsGenericTypeDefinition => false;
    
    public bool IsGenericTypeParameter => false;

    public Type AsType() => elementType.AsType().MakeArrayType();

    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);

    public ITypeInfo ElementType() => elementType;

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];

    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    public bool Equals(ITypeInfo? other)
    {
        if (other == null) return false;
        if (!other.IsArrayType) return false;
        if (other is not ArrayTypeBuilder otherBuilder) return false;
        return elementType.Equals(otherBuilder.ElementType());
    }
}