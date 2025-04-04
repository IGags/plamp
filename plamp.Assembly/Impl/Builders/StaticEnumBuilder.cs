using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Assembly.Impl.Models;

namespace plamp.Assembly.Impl.Builders;

public class StaticEnumBuilder
{
    public Type EnumType { get; }

    private readonly List<EnumFieldInfo> _fields = [];
    
    internal IReadOnlyList<EnumFieldInfo> Fields => _fields;

    internal StaticEnumBuilder(Type enumType)
    {
        EnumType = enumType;
    }
    
    public void DefineField(FieldInfo field, string alias = null)
    {
        if (field.DeclaringType != EnumType)
            throw new InvalidOperationException($"Field must belong to the {EnumType.Name}");

        alias ??= field.Name;
        
        var existingField = _fields.FirstOrDefault(x => x.Field == field);
        if (existingField != default)
        {
            if(existingField.Alias.Equals(alias)) return;
            throw new InvalidOperationException(GetAliasExMessage(alias));
        }
        
        existingField = _fields.FirstOrDefault(x => x.Alias.Equals(alias));
        if (existingField != default) throw new InvalidOperationException(GetAliasExMessage(alias));
        var info = new EnumFieldInfo(field, alias);
        _fields.Add(info);
        return;
        
        static string GetAliasExMessage(string alias) 
            => $"Field with alias '{alias}' and different value already declared.";
    }
}