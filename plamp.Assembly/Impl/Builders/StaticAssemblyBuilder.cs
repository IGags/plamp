using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Impl.Models;

namespace plamp.Assembly.Impl.Builders;

public class StaticAssemblyBuilder
{
    public System.Reflection.Assembly Assembly { get; }

    private readonly List<TypeBuilderInfo> _types = [];
    
    private readonly List<EnumBuilderInfo> _enums = [];
    
    internal IReadOnlyList<TypeBuilderInfo> Types => _types;
    
    internal IReadOnlyList<EnumBuilderInfo> Enums => _enums;

    internal StaticAssemblyBuilder(System.Reflection.Assembly assembly)
    {
        Assembly = assembly;
    }
    
    public StaticTypeBuilder DefineType(Type type, string alias = null, string namespaceOverride = null)
    {
        if(type.IsEnum) throw new InvalidOperationException($"The type can't be an enum.");
        alias = GetAlias(type, alias);
        namespaceOverride = GetNamespaceOverride(type, namespaceOverride);

        if (_enums.Any(x => x.Alias.Equals(alias) && x.NamespaceOverride.Equals(namespaceOverride)))
        {
            throw new InvalidOperationException("Assembly already contains an enum. With the same name and namespace.");
        }

        var typ = _types.FirstOrDefault(x => x.TypeBuilder.Type == type);
        if (typ != default)
        {
            if (!typ.NamespaceOverride.Equals(namespaceOverride) || !typ.Alias.Equals(alias))
            {
                throw new InvalidOperationException(
                    $"The type with alias '{alias}' and actual name {typ.TypeBuilder.Type.Name} already exists.");
            }

            return typ.TypeBuilder;
        }

        var builder = new StaticTypeBuilder(type);
        typ = new TypeBuilderInfo(builder, alias, namespaceOverride);
        _types.Add(typ);
        return typ.TypeBuilder;
    }

    //TODO: Correct inheritance
    public StaticEnumBuilder DefineEnum(Type enumType, string alias = null, string namespaceOverride = null)
    {
        if(!enumType.IsEnum) throw new InvalidOperationException($"The type must be an enum.");
        alias = GetAlias(enumType, alias);
        namespaceOverride = GetNamespaceOverride(enumType, namespaceOverride);

        if (_types.Any(x => x.Alias.Equals(alias) && x.NamespaceOverride.Equals(namespaceOverride)))
        {
            throw new InvalidOperationException("Assembly already contains type. With the same name and namespace.");
        }

        var enumInfo = _enums.FirstOrDefault(x => x.TypeBuilder.EnumType == enumType);
        if (enumInfo != default)
        {
            if (!enumInfo.NamespaceOverride.Equals(namespaceOverride) || !enumInfo.Alias.Equals(alias))
            {
                throw new InvalidOperationException(
                    $"Enum with alias '{alias}' and actual name {enumInfo.TypeBuilder.EnumType.Name} already exists.");
            }
            
            return enumInfo.TypeBuilder;
        }
        
        var builder = new StaticEnumBuilder(enumType);
        enumInfo = new EnumBuilderInfo(builder, alias, namespaceOverride);
        _enums.Add(enumInfo);
        return enumInfo.TypeBuilder;
    }

    private string GetAlias(Type type, string alias = null) => alias ?? type.Name;
    
    private string GetNamespaceOverride(Type type, string nsOverride = null) => nsOverride ?? type.Namespace;
}