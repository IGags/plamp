using System;
using System.Reflection;

namespace plamp.Abstractions.Assemblies;

public interface IStaticAssemblyContainer
{
    Assembly GetAssembly(AssemblyName name);
    
    Type GetType(string name, string nameSpace, AssemblyName assemblyName);

    Type GetType(string name, string nameSpace, Assembly assembly);

    MethodInfo[] GetMethodSignatures(string name, Type type);
    
    MethodInfo[] GetMethodSignatures(string name, string typeName, string nameSpace, AssemblyName assemblyName);

    FieldInfo GetField(string name, Type type);
    
    FieldInfo GetField(string name, string type, string nameSpace, AssemblyName assemblyName);
    
    PropertyInfo GetProperty(string name, Type type);
    
    PropertyInfo GetProperty(string name, string type, string nameSpace, AssemblyName assemblyName);
    
    Type GetEnum(string name, string nameSpace, AssemblyName assemblyName);

    Type GetEnum(string name, string nameSpace, Assembly assembly);
}