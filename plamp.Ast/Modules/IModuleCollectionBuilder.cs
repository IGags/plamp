using System;
using System.Reflection;

namespace plamp.Ast.Modules;

/// <summary>
/// Module builder creates before main validators pipelines
/// </summary>
//TODO: Update and remove api(override possible)
public interface IModuleCollectionBuilder
{
    /// <summary>
    /// Define new module if it does not exist
    /// </summary>
    public bool TryDefineModule(string moduleName);
    
    /// <summary>
    /// Adds dependency from one module to another explicitly.
    /// In default implementation of plamp strongly required for reference from one c# static module to another
    /// </summary>
    public bool AddModuleDependencies(string module, params string[] dependencies);
    
    /// <summary>
    /// Adds compiled method and transient dependencies required for it,
    /// otherwise if method with same signatures exists returns false
    /// </summary>
    public bool TryAddCompiledMethod(string moduleName, MethodInfo compiledMethod);
    
    /// <summary>
    /// Adds compiled type and transient dependencies required for it,
    /// otherwise if type exists returns false
    /// </summary>
    public bool TryAddCompiledType(string moduleName, Type compiledType);
    
    /// <summary>
    /// Adds compiled field and transient dependencies required for it,
    /// otherwise if field exists returns false
    /// </summary>
    public bool TryAddCompiledField(string moduleName, FieldInfo compiledField);
    
    /// <summary>
    /// Adds compiled property and transient dependencies required for it,
    /// otherwise if property exists returns false
    /// </summary>
    public bool TryAddCompiledProperty(string moduleName, PropertyInfo compiledProperty);
    
    /// <summary>
    /// Adds method definition from script file e.g. symbol table,
    /// otherwise returns false if method already exists
    /// </summary>
    /// <param name="moduleName">Module method belongs to</param>
    /// <param name="callerTypeFullName">Type contains this method</param>
    /// <param name="methodName">The name of this method</param>
    /// <param name="returnTypeFullName">Module + type that return method</param>
    /// <param name="parameterTypeFullNames">Module + type [] that accepts method</param>
    /// <returns>Was method added</returns>
    public bool TryAddMethodDefinition(
        string moduleName, string callerTypeFullName, string methodName, 
        TypeFullName returnTypeFullName, params TypeFullName[] parameterTypeFullNames);
    
    /// <summary>
    /// Adds type definition from script file e.g. symbol table,
    /// otherwise returns false if type already exists
    /// </summary>
    /// <param name="fullName">Module method belongs to and type name themself</param>
    /// <returns>Was type added</returns>
    public bool TryTypeDefinition(TypeFullName fullName);
    
    /// <summary>
    /// Builds modules into collection
    /// </summary>
    public IModuleCollection Build();
}