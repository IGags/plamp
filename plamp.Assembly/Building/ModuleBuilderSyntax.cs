using System;
using System.Collections.Generic;
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

    public IAfterTypeInfoNameBuilder<T> AddGenericTypeDefinition<T>()
    {
        var type = typeof(T);
        if (!type.IsGenericType) throw new ArgumentException($"Type {type.Name} is not generic type");
        var definition = type.GetGenericTypeDefinition();
        var typeInfo = AddTypeToDictionary(definition);
        return new TypeBuilderFluentSyntax<T>(typeInfo, this);
    }

    private DefaultTypeInfo AddTypeToDictionary(Type type)
    {
        if (containerBuilder.TypeInfoDict.TryGetValue(type, out var info) && info.Module != moduleName) 
            throw new ArgumentException($"Type already exists in container in module {info.Module} as {info.Alias}");
        
        DefaultTypeInfo typeInfo;
        if (info == null)
        {
            typeInfo = new DefaultTypeInfo(type.Name, type, moduleName);
            containerBuilder.TypeInfoDict.Add(type, typeInfo);
            _definedTypes.Add(typeInfo);
        }
        else
        {
            typeInfo = info;
            typeInfo.Alias = type.Name;
        }
        
        return typeInfo;
    }

    public IContainerBuilder CompleteModule() => containerBuilder;
}