using System;
using System.Reflection;

namespace plamp.Ast.Assemblies;

public interface IStaticAssemblyContainer
{
    Assembly GetAssembly(string name);
    
    Type GetType(string name, string nameSpace, string assemblyName);

    Type GetType(string name, string nameSpace, Assembly assembly);

    MethodInfo[] GetMethodSignatures(string name, Type type);
    
    MethodInfo[] GetMethodSignatures(string name, string typeName, string nameSpace, string assemblyName);

    FieldInfo GetField(string name, Type type);
    
    FieldInfo GetField(string name, string type, string nameSpace, string assemblyName);
    
    PropertyInfo GetProperty(string name, Type type);
    
    PropertyInfo GetProperty(string name, string type, string nameSpace, string assemblyName);
    
    Type GetEnum(string name, string nameSpace, string assemblyName);

    Type GetEnum(string name, string nameSpace, Assembly assembly);
}