using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingTests
{
    [Fact]
    public void CreateEmptyContainer()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes();
        Assert.Empty(types);
    }

    [Fact]
    public void CreateEmptyModule()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        _ = builder.DefineModule("mod1");
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes();
        Assert.Empty(types);
    }

    [Fact]
    public void CreateSingleType()
    {
        const string moduleName = "mod1";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule(moduleName).AddType<object>().CompleteType().CompleteModule();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        Assert.Single(types);
        var type = types.First();
        Assert.Equal(typeof(object), type.Type);
        Assert.Equal(moduleName, type.Module);
        Assert.Null(type.Alias);
    }

    [Fact]
    public void CreateSingleTypeWithAlias()
    {
        const string moduleName = "mod1";
        const string aliasName = "IntIntPair";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule(moduleName)
            .AddType<KeyValuePair<int, int>>().As(aliasName).CompleteType().CompleteModule();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        Assert.Single(types);
        var type = types.First();
        Assert.Equal(typeof(KeyValuePair<int, int>), type.Type);
        Assert.Equal(moduleName, type.Module);
        Assert.Equal(aliasName, type.Alias);
    }

    [Fact]
    public void CreateSingleTypeWithoutComplete()
    {
        const string moduleName = "mod1";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule(moduleName).AddType<object>();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        Assert.Single(types);
        var type = types.First();
        Assert.Equal(typeof(object), type.Type);
        Assert.Equal(moduleName, type.Module);
        Assert.Null(type.Alias);
    }

    public void CreateMultipleNonCollideTypesInTheSameModule()
    {
        
    }
}