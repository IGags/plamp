using System;
using System.Linq;
using System.Reflection;
using plamp.Assembly.Impl.BuiltRecords;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Impl;

internal class DefaultStaticAssemblyContainer : IStaticAssemblyContainer
{
    private readonly PreparedAssembly[] _assemblies;

    internal DefaultStaticAssemblyContainer(PreparedAssembly[] assemblies)
    {
        _assemblies = assemblies;
    }
    
    public System.Reflection.Assembly GetAssembly(AssemblyName name)
    {
        return _assemblies.FirstOrDefault(x => x.Alias.Equals(name)).Assembly;
    }

    public Type GetType(string name, string nameSpace, AssemblyName assemblyName)
    {
        var prepared = _assemblies.FirstOrDefault(x => x.Alias.Equals(assemblyName));
        return prepared.Types.FirstOrDefault(x => x.Alias.Equals(name) && x.Namespace.Equals(nameSpace)).Type;
    }

    public Type GetType(string name, string nameSpace, System.Reflection.Assembly assembly)
    {
        var prepared = _assemblies.FirstOrDefault(x => x.Assembly == assembly);
        return prepared.Types.FirstOrDefault(x => x.Alias.Equals(name) && x.Namespace.Equals(nameSpace)).Type;
    }

    public MethodInfo[] GetMethodSignatures(string name, Type type)
    {
        var assembly = _assemblies.FirstOrDefault(x => x.Assembly.Equals(type.Assembly));
        
        return assembly.Types
            .FirstOrDefault(x => x.Type == type).Members
            .Where(x => x.Alias.Equals(name) && x.Info is MethodInfo)
            .Select(x => (MethodInfo)x.Info).ToArray();
    }

    public MethodInfo[] GetMethodSignatures(string name, string typeName, string nameSpace, AssemblyName assemblyName)
    {
        var assembly = _assemblies.FirstOrDefault(x => x.Alias.Equals(assemblyName));
        var type = assembly.Types.FirstOrDefault(x => x.Alias.Equals(typeName) && x.Namespace.Equals(nameSpace));
        return type.Members
            .Where(x => x.Alias.Equals(name) && x.Info is MethodInfo)
            .Select(x => (MethodInfo)x.Info).ToArray();
    }

    public FieldInfo GetField(string name, Type type)
    {
        throw new NotImplementedException();
    }

    public FieldInfo GetField(string name, string type, string nameSpace, AssemblyName assemblyName)
    {
        throw new NotImplementedException();
    }

    public PropertyInfo GetProperty(string name, Type type)
    {
        throw new NotImplementedException();
    }

    public PropertyInfo GetProperty(string name, string type, string nameSpace, AssemblyName assemblyName)
    {
        throw new NotImplementedException();
    }

    public Type GetEnum(string name, string nameSpace, AssemblyName assemblyName)
    {
        throw new NotImplementedException();
    }

    public Type GetEnum(string name, string nameSpace, System.Reflection.Assembly assembly)
    {
        throw new NotImplementedException();
    }
}