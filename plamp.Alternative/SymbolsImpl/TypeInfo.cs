using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.SymbolsImpl;

public class TypeInfo : ITypeInfo
{
    private readonly Type _type;
    private readonly string? _nameOverride;

    public Type AsType() => _type;

    public IReadOnlyList<IFieldInfo> Fields { get; init; }

    public string Name { get; }

    public bool IsArrayType => _type.IsArray;
    
    public TypeInfo(Type type, string? nameOverride = null)
    {
        _type = type;
        _nameOverride = nameOverride;
        Name = MakeName(nameOverride ?? type.Name);
        Fields = type.GetFields()
            //TODO: попахивает костылём для ограничения полей из базовых типов .net runtime/
            .Where(x => x.GetCustomAttribute<PlampFieldGeneratedAttribute>() != null)
            .Select(x => new FldInfo(x)).ToList();
    }
    
    public ITypeInfo MakeArrayType()
    {
        var arrayType = _type.MakeArrayType();
        return _nameOverride == null ? new TypeInfo(arrayType) : new TypeInfo(arrayType, _nameOverride);
    }

    public ITypeInfo? ElementType()
    {
        if (!_type.IsArray) return null;
        var elemType = _type.GetElementType()!;
        return _nameOverride == null ? new TypeInfo(elemType) : new TypeInfo(elemType, _nameOverride);
    }

    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeInfo typ) return false;
        return typ._type == _type;
    }

    private string MakeName(string baseName)
    {
        var typ = _type;
        while (typ.IsArray)
        {
            baseName = "[]" + baseName;
            typ = typ.GetElementType()!;
        }

        return baseName;
    }
}