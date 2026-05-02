using System;
using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc/>
public class ArrayTypeBuilder(ITypeInfo elementType) : ITypeInfo
{
    /// <inheritdoc/>
    public IReadOnlyList<IFieldInfo> Fields => [];

    /// <inheritdoc/>
    public string Name => "[]" + elementType.Name;
    
    /// <inheritdoc/>
    public string ModuleName => elementType.ModuleName;

    /// <inheritdoc/>
    public string DefinitionName => elementType.DefinitionName;

    /// <inheritdoc/>
    public bool IsArrayType => true;

    /// <inheritdoc/>
    public bool IsGenericType => false;

    /// <inheritdoc/>
    public bool IsGenericTypeDefinition => false;
    
    /// <inheritdoc/>
    public bool IsGenericTypeParameter => false;

    /// <inheritdoc/>
    public Type AsType() => elementType.AsType().MakeArrayType();

    /// <inheritdoc/>
    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);
    
    /// <inheritdoc/>
    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments) => null;

    /// <inheritdoc/>
    public ITypeInfo ElementType() => elementType;

    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericParameters() => [];
    
    /// <inheritdoc/>
    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    /// <inheritdoc/>
    public ITypeInfo? GetGenericTypeDefinition() => null;
    
    /// <inheritdoc/>
    public bool Equals(ITypeInfo? other)
    {
        if (other == null) return false;
        if (!other.IsArrayType) return false;
        if (other is not ArrayTypeBuilder otherBuilder) return false;
        return elementType.Equals(otherBuilder.ElementType());
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(GetType());
        code.Add(elementType.GetHashCode());
        code.Add("[]");
        return code.ToHashCode();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not ITypeInfo other) return false;
        return Equals(other);
    }
}