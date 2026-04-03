using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Alternative.SymbolsBuildingImpl;

namespace plamp.Alternative.SymbolsImpl;

public class TypeInfo : ITypeInfo
{
    private readonly Type _type;
    private readonly string? _nameOverride;
    
    public Type AsType() => _type;

    public IReadOnlyList<IFieldInfo> Fields { get; }

    public string Name { get; }

    public string GenericDefinitionName
    {
        get
        {
            if (!_type.IsGenericTypeDefinition) throw new InvalidOperationException("Тип не является объявлением дженерик типа");
            return _type.Name.Split('`', StringSplitOptions.RemoveEmptyEntries)[0];
        }
    }

    public bool IsArrayType => _type.IsArray;

    public bool IsGenericType => _type.IsGenericType;

    public bool IsGenericTypeDefinition => _type.IsGenericTypeDefinition;

    public bool IsGenericTypeParameter => _type.IsGenericMethodParameter;

    public TypeInfo(Type type, string? nameOverride = null)
    {
        _type = type;
        _nameOverride = nameOverride;
        Name = MakeName(type, nameOverride);
        Fields = type.GetFields()
            //TODO: попахивает костылём для ограничения полей из базовых типов .net runtime
            .Where(x => x.GetCustomAttribute<PlampVisibleAttribute>() != null)
            .Select(x => new FldInfo(x)).ToList();
    }
    
    public ITypeInfo MakeArrayType()
    {
        var arrayType = _type.MakeArrayType();
        return _nameOverride == null ? new TypeInfo(arrayType) : new TypeInfo(arrayType, _nameOverride);
    }

    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (!_type.IsGenericTypeDefinition) return null;
        
        return new GenericTypeBuilder(this, genericTypeArguments);
    }

    public ITypeInfo? ElementType()
    {
        if (!_type.IsArray) return null;
        var elemType = _type.GetElementType()!;
        return _nameOverride == null ? new TypeInfo(elemType) : new TypeInfo(elemType, _nameOverride);
    }

    public IReadOnlyList<ITypeInfo> GetGenericParameters()
    {
        if (!IsGenericTypeDefinition) return [];
        return _type.GetGenericArguments().Select(x => new TypeInfo(x)).ToList();
    }

    public IReadOnlyList<ITypeInfo> GetGenericArguments()
    {
        if(!IsGenericTypeDefinition) return [];
        return _type.GetGenericArguments().Select(x => new TypeInfo(x)).ToList();
    }

    public ITypeInfo? GetGenericTypeDefinition()
    {
        if (!IsGenericType) return null;
        var def = _type.GetGenericTypeDefinition();
        return new TypeInfo(def);
    }

    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeInfo typ) return false;
        return typ._type == _type;
    }

    private string MakeName(Type type, string? nameOverride = null)
    {
        if (type.IsGenericTypeDefinition || type.IsGenericType)
        {
            throw new NotSupportedException();
        }

        var arrayNesting = 0;
        while (type.IsArray)
        {
            type = type.GetElementType()!;
            arrayNesting++;
        }

        var typeName = nameOverride ?? type.Name;
        typeName = string.Concat(Enumerable.Repeat("[]", arrayNesting)) + typeName;
        return typeName;
    }

    public override int GetHashCode()
    {
        return _type.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return _type.Equals(obj);
    }
}