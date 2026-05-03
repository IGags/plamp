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
    
    public Type AsType() => _type;

    public IReadOnlyList<IFieldInfo> Fields { get; }
    
    public string Name { get; }
    
    public string ModuleName { get; }

    public string DefinitionName { get; }

    public bool IsArrayType => _type.IsArray;

    public bool IsGenericType => _type is { IsGenericType: true, IsGenericTypeDefinition: false };

    public bool IsGenericTypeDefinition => _type.IsGenericTypeDefinition;

    public bool IsGenericTypeParameter => _type.IsGenericMethodParameter;

    private TypeInfo(Type type, string moduleName, string? nameOverride = null)
    {
        if (type is { IsGenericType: true, IsGenericTypeDefinition: false }) throw new ArgumentException("Нельзя использовать дженерик тип как тип в таблице символов");
        if (type.IsArray) throw new ArgumentException("Нельзя использовать тип массива как тип в таблице символов");
        ModuleName = moduleName;
        
        _type = type;
        nameOverride ??= type.Name.Split('`', StringSplitOptions.RemoveEmptyEntries)[0];
        DefinitionName = nameOverride;

        if (type.IsGenericTypeDefinition)
        {
            Name = nameOverride + $"[{string.Join(", ", type.GetGenericArguments().Select(t => t.Name))}]";
        }
        else
        {
            Name = nameOverride;
        }
        
        Fields = type.GetFields()
            //TODO: попахивает костылём для ограничения полей из базовых типов .net runtime
            .Where(x => x.GetCustomAttribute<PlampVisibleAttribute>() != null)
            .Select(x => new FldInfo(x, moduleName)).ToList();
    }

    public static ITypeInfo FromType(Type type, string moduleName, string? nameOverride = null)
    {
        var arrayCt = 0;
        while (type.IsArray)
        {
            type = type.GetElementType()!;
            arrayCt++;
        }

        ITypeInfo[]? infos = null; 
        if (type is { IsGenericType: true, IsGenericTypeDefinition: false })
        {
            var args = type.GetGenericArguments();
            infos = args.Select(x => FromType(x, moduleName)).ToArray();
            type = type.GetGenericTypeDefinition();
        }

        ITypeInfo defInfo = new TypeInfo(type, moduleName, nameOverride);
        ITypeInfo? genericInfo;
        if (infos != null && (genericInfo = defInfo.MakeGenericType(infos)) != null)
        {
            defInfo = genericInfo;
        }

        for (; 0 < arrayCt; arrayCt--)
        {
            defInfo = defInfo.MakeArrayType();
        }

        return defInfo;
    }
    
    public ITypeInfo MakeArrayType() => new ArrayTypeBuilder(this);

    public ITypeInfo? MakeGenericType(IReadOnlyList<ITypeInfo> genericTypeArguments)
    {
        if (!_type.IsGenericTypeDefinition) return null;
        return new GenericTypeBuilder(this, genericTypeArguments);
    }

    public ITypeInfo? ElementType() => null;

    public IReadOnlyList<ITypeInfo> GetGenericParameters()
    {
        if (!IsGenericTypeDefinition) return [];
        return _type.GetGenericArguments().Select(x => new TypeInfo(x, ModuleName)).ToList();
    }

    public IReadOnlyList<ITypeInfo> GetGenericArguments() => [];

    public ITypeInfo? GetGenericTypeDefinition()
    {
        if (!IsGenericType) return null;
        var def = _type.GetGenericTypeDefinition();
        return new TypeInfo(def, ModuleName);
    }

    public bool Equals(ITypeInfo? other)
    {
        if (other is not TypeInfo typ) return false;
        return typ._type == _type;
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