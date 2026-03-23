using System;
using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class ArrayTypeBuilder(ITypeInfo elementType) : ITypeInfo
{
    public IReadOnlyList<IFieldInfo> Fields => [];

    public string Name => "[]" + elementType.Name;

    public bool IsArrayType => true;

    public bool IsGenericType => false;

    public bool IsGenericTypeDefinition => false;
    
    public bool IsGenericTypeParameter => false;

    public Type AsType() => elementType.AsType().MakeArrayType();

    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    public ITypeInfo ElementType() => elementType;

    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    public bool Equals(ITypeInfo? other)
    {
        if (other == null) return false;
        if (!other.IsArrayType) return false;
        if (other is not ArrayTypeBuilder otherBuilder) return false;
        return elementType.Equals(otherBuilder.ElementType());
    }

    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(elementType.GetHashCode());
        code.Add("[]");
        return code.ToHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }
}