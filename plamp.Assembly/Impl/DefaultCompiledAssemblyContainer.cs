using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Assembly.Impl.BuiltRecords;
using plamp.Abstractions.Assemblies;

namespace plamp.Assembly.Impl;

internal class DefaultCompiledAssemblyContainer : ICompiledAssemblyContainer
{
    private readonly PreparedAssembly[] _assemblies;

    internal DefaultCompiledAssemblyContainer(PreparedAssembly[] assemblies)
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

    public Type GetType(string fullName)
    {
        var nameSplitter = fullName.LastIndexOf('.');
        var typeName = nameSplitter == -1 ? fullName : fullName.Substring(nameSplitter + 1);
        var @namespace = nameSplitter == -1 ? "" : fullName.Substring(0, nameSplitter);

        var type = _assemblies
            .Select(x => x.Types)
            .SelectMany(x => x)
            .FirstOrDefault(x => x.Namespace.Equals(@namespace) && x.Alias.Equals(typeName));
        
        return type == default ? null : type.Type;
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

    public ICompiledAssemblyContainer AddCompiledDynamicAssembly(System.Reflection.Assembly assembly)
    {
        var types = assembly.GetExportedTypes();
        var nonEnums = types.Where(x => !x.IsEnum).ToList();
        var enums = types.Where(x => x.IsEnum).ToList();
        var typeInfos = nonEnums.Select(x =>
        {
            var methods = x.GetMethods(BindingFlags.Public);
            var fields =x.GetFields(BindingFlags.Public);
            var properties = x.GetProperties(BindingFlags.Public);
            var members = new PreparedMember[methods.Length + fields.Length + properties.Length];
            methods.CopyTo(members, 0);
            fields.CopyTo(members, methods.Length);
            properties.CopyTo(properties, methods.Length + fields.Length);
            
            return new PreparedType()
            {
                Alias = x.Name,
                Members = members,
                Namespace = x.Namespace,
                Type = x
            };
        });
        var enumInfos = enums.Select(x =>
        {
            var fields = x.GetFields(BindingFlags.Public)
                .Select(y => new PreparedMember(y, y.Name)).ToArray();

            return new PreparedType()
            {
                Alias = x.Name,
                Members = fields,
                Namespace = x.Namespace,
                Type = x
            };
        });
        var preparedTypes = typeInfos.Concat(enumInfos).ToArray();
        
        var preparedAssemblies = new PreparedAssembly[_assemblies.Length + 1];
        _assemblies.CopyTo(preparedAssemblies, 0);
        preparedAssemblies[^1] = new PreparedAssembly(assembly, assembly.GetName(), preparedTypes);

        return new DefaultCompiledAssemblyContainer(preparedAssemblies);
    }
}