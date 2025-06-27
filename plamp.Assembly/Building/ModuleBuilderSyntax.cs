using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Building.Interfaces;
using plamp.Assembly.Models;

namespace plamp.Assembly.Building;

internal class ModuleBuilderSyntax(string moduleName, NativeAssemblyContainerBuilder containerBuilder) : IModuleBuilderSyntax
{
    private readonly List<DefaultTypeInfo> _definedTypes = [];

    public NativeAssemblyContainerBuilder ContainerBuilder => containerBuilder;
    
    public IReadOnlyList<DefaultTypeInfo> DefinedTypes => _definedTypes; 
    
    public IAfterTypeInfoNameBuilder<T> AddType<T>()
    {
        var typeInfo = AddTypeToDictionary(typeof(T));
        return new TypeBuilderFluentSyntax<T>(typeInfo, this);
    }

    public IAfterTypeInfoNameBuilder AddType(Type type)
    {
        throw new NotImplementedException();
    }

    public IAfterTypeInfoNameBuilder<T> AddGenericTypeDefinition<T>()
    {
        var type = typeof(T);
        if (!type.IsGenericType) throw new ArgumentException($"Type {type.Name} is not generic type");
        var definition = type.GetGenericTypeDefinition();
        var typeInfo = AddTypeToDictionary(definition);
        return new TypeBuilderFluentSyntax<T>(typeInfo, this);
    }

    public IAfterTypeInfoNameBuilder AddGenericTypeDefinition(Type type)
    {
        throw new NotImplementedException();
    }

    public void ThrowDuplicateModuleAlias(string alias, Type thisType)
    {
        //Alias shouldn't be empty
        if (
            DefinedTypes.Where(x => x.Type != thisType).Any(x => alias.Equals(x.Alias) 
                                                                 && GetTypeName(thisType) != GetTypeName(x.Type)))
        {
            throw new ArgumentException($"Type with the name {alias} already declared in this module");
        }
    }

    public void ThrowMemberNameEquality(string alias, Type type)
    {
        if(!containerBuilder.TypeInfoDict.TryGetValue(type, out var typeInfo)) return;
        if(containerBuilder.FieldInfoDict.TryGetValue(typeInfo, out var fields))
        {
            if (fields.Any(x => x.Alias == alias)) throw new ArgumentException($"Type {type.Name} cannot be named as enclosing field {alias}");
        }

        if (containerBuilder.PropInfoDict.TryGetValue(typeInfo, out var properties))
        {
            if (properties.Any(x => x.Alias == alias)) throw new ArgumentException($"Type cannot {type.Name} be named as enclosing property {alias}");
        }

        if (containerBuilder.MethodInfoDict.TryGetValue(typeInfo, out var methods))
        {
            if (methods.Any(x => x.Alias == alias)) throw new ArgumentException($"Type cannot {type.Name} be named as enclosing method {alias}");
        }
    }

    public IContainerBuilder CompleteModule() => containerBuilder;

    private DefaultTypeInfo AddTypeToDictionary(Type type)
    {
        if (containerBuilder.TypeInfoDict.TryGetValue(type, out var info) && info.Module != moduleName) 
            throw new ArgumentException($"Type already exists in container in module {info.Module} as {info.Alias}");

        var typeName = GetTypeName(type);
        ThrowDuplicateModuleAlias(typeName, type);
        ThrowMemberNameEquality(typeName, type);
        
        DefaultTypeInfo typeInfo;
        if (info == null)
        {
            typeInfo = new DefaultTypeInfo(typeName, type, moduleName);
            containerBuilder.TypeInfoDict.Add(type, typeInfo);
            _definedTypes.Add(typeInfo);
        }
        else
        {
            typeInfo = info;
            typeInfo.Alias = typeName;
        }
        
        return typeInfo;
    }

    //To get same name just use nameof intrinsic

    private string GetTypeName(Type type)
    {
        if (!type.IsGenericType) return type.Name;
        var name = type.Name;
        var index = name.IndexOf('`');
        return name[..index];
    }
}