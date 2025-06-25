using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Assemblies;
using plamp.Assembly.Building.Interfaces;
using plamp.Assembly.Models;

namespace plamp.Assembly.Building;

public class NativeAssemblyContainerBuilder : IContainerBuilder
{
    internal Dictionary<Type, DefaultTypeInfo> TypeInfoDict { get; } = [];

    internal Dictionary<DefaultTypeInfo, List<DefaultMethodInfo>> MethodInfoDict { get; } = [];

    internal Dictionary<DefaultTypeInfo, List<DefaultFieldInfo>> FieldInfoDict { get; } = [];

    internal Dictionary<DefaultTypeInfo, List<DefaultConstructorInfo>> CtorInfoDict { get; } = [];

    internal Dictionary<DefaultTypeInfo, List<DefaultPropertyInfo>> PropInfoDict { get; } = [];

    internal Dictionary<DefaultTypeInfo, List<DefaultIndexerInfo>> IndexerInfoDict { get; } = [];

    public static IContainerBuilder CreateContainerBuilder()
    {
        return new NativeAssemblyContainerBuilder();
    }

    public IModuleBuilderSyntax DefineModule(string moduleName) => new ModuleBuilderSyntax(moduleName, this);
    
    public IAssemblyContainer CreateContainer()
    {
        //Full copy :8|
        return new DefaultAssemblyContainer()
        {
            Constructors = CtorInfoDict.ToDictionary(x => (ITypeInfo)x.Key, x => x.Value),
            Methods = MethodInfoDict.ToDictionary(x => (ITypeInfo)x.Key, x => x.Value),
            Properties = PropInfoDict.ToDictionary(x => (ITypeInfo)x.Key, x => x.Value),
            Fields = FieldInfoDict.ToDictionary(x => (ITypeInfo)x.Key, x => x.Value),
            Types = TypeInfoDict
                .GroupBy(x => x.Value.Alias)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList()),
            Indexers = IndexerInfoDict.ToDictionary(x => (ITypeInfo)x.Key, x => x.Value)
        };
    }
}