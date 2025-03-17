using System;
using System.Collections.Generic;
using System.Reflection;
using plamp.Assembly.Implementations.Dynamic;
using plamp.Assembly.Implementations.Dynamic.Symbols;
using plamp.Assembly.Implementations.Static;
using plamp.Ast.Modules;

namespace plamp.Assembly.Implementations;

public class ModuleCollectionBuilder : IModuleCollectionBuilder
{
    private Dictionary<string, DynamicModuleDefinition> _dynamicModules = [];
    private Dictionary<string, StaticModuleDefinition> _staticModules = [];
    
    public bool TryAddBaseCollection(IModuleCollection collection)
    {
        throw new NotImplementedException();
    }

    public bool TryDefineModule(string moduleName)
    {
        throw new NotImplementedException();
    }

    public bool AddModuleDependencies(string module, params string[] dependencies)
    {
        throw new NotImplementedException();
    }

    public bool TryAddCompiledMethod(string moduleName, MethodInfo compiledMethod)
    {
        throw new NotImplementedException();
    }

    public bool TryAddCompiledType(string moduleName, Type compiledType)
    {
        throw new NotImplementedException();
    }

    public bool TryAddCompiledField(string moduleName, FieldInfo compiledField)
    {
        throw new NotImplementedException();
    }

    public bool TryAddCompiledProperty(string moduleName, PropertyInfo compiledProperty)
    {
        throw new NotImplementedException();
    }

    public bool TryAddMethodDefinition(TypeFullName callerTypeFullName, string methodName,
        TypeFullName returnTypeFullName, params TypeFullName[] parameterTypeFullNames)
    {
        if(!_dynamicModules.TryGetValue(callerTypeFullName.ModuleName, out var dynamicModule)) 
            return false;
        if(dynamicModule.FindType(callerTypeFullName.TypeName) is not DynamicTypeDefinition type) 
            return false;
        
        return type.TryAddMethod()
    }

    public bool TryTypeDefinition(TypeFullName fullName)
    {
        
    }

    public IModuleCollection Build()
    {
        throw new NotImplementedException();
    }
}