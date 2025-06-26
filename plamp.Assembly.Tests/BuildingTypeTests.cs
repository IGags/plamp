using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingTypeTests
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
        Assert.Equal(nameof(Object), type.Alias);
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
        Assert.Equal(nameof(Object), type.Alias);
    }

    [Fact]
    public void CreateMultipleNonCollideTypesInSameModule()
    {
        const string moduleName = "mod1";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule(moduleName)
            .AddType<DateTime>().CompleteType()
            .AddType<TimeSpan>().CompleteType();
        var container = builder.CreateContainer();
        var dateTimeList = container.GetMatchingTypes(nameof(DateTime));
        var timeSpanList = container.GetMatchingTypes(nameof(TimeSpan));
        var types = container.EnumerateTypes().ToList();
        var dateTime = dateTimeList.Single();
        var timeSpan = timeSpanList.Single();
        Assert.Equal(2, types.Count);
        Assert.NotNull(dateTime);
        Assert.NotNull(timeSpan);
        Assert.Equal(typeof(DateTime), dateTime.Type);
        Assert.Equal(nameof(DateTime), dateTime.Alias);
        Assert.Equal(moduleName, dateTime.Module);
        Assert.Equal(typeof(TimeSpan), timeSpan.Type);
        Assert.Equal(nameof(TimeSpan), timeSpan.Alias);
        Assert.Equal(moduleName, timeSpan.Module);
    }

    [Fact]
    public void CreateTypeTwiceInTheSameModule()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("123")
            .AddType<object>().CompleteType()
            .AddType<object>();
        var container = builder.CreateContainer();
        Assert.Single(container.EnumerateTypes());
    }

    [Fact]
    public void CreateSameTypeWithDifferentAliasInTheSameModule()
    {
        const string moduleName = "mod1";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule(moduleName)
            .AddType<object>().As("notObj").CompleteType()
            .AddType<object>();
        var container = builder.CreateContainer();
        var type = container.EnumerateTypes().Single();
        Assert.Equal(typeof(object), type.Type);
        Assert.Equal(nameof(Object), type.Alias);
        Assert.Equal(moduleName, type.Module);
    }

    [Fact]
    public void CreateSameTypeInDifferentModule()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder
            .DefineModule("1")
                .AddType<object>().CompleteType().CompleteModule()
            .DefineModule("2");
        Assert.Throws<ArgumentException>(() => syntax.AddType<object>());
    }

    [Fact]
    public void CreateTwoDifferentTypesInTwoModules()
    {
        const string mod1Name = "1";
        const string mod2Name = "2";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder
            .DefineModule(mod1Name).AddType<int>().CompleteType().CompleteModule()
            .DefineModule(mod2Name).AddType<short>().CompleteType().CompleteModule();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        var integer = container.GetMatchingTypes(nameof(Int32)).Single();
        var shortType = container.GetMatchingTypes(nameof(Int16)).Single();
        Assert.Equal(2, types.Count);
        
        Assert.Equal(typeof(int), integer.Type);
        Assert.Equal(nameof(Int32), integer.Alias);
        Assert.Equal(mod1Name, integer.Module);
        
        Assert.Equal(typeof(short), shortType.Type);
        Assert.Equal(nameof(Int16), shortType.Alias);
        Assert.Equal(mod2Name, shortType.Module);
    }

    [Fact]
    public void CreateSameAliasInSameModule()
    {
        const string alias = "date";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1")
            .AddType<DateTime>().As(alias).CompleteType()
            .AddType<TimeSpan>();
        Assert.Throws<ArgumentException>(() => syntax.As(alias));
    }

    [Fact]
    public void CreateSameAliasInDifferentModule()
    {
        const string alias = "date";
        const string module1Name = "mod1";
        const string module2Name = "mod2";

        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder
            .DefineModule(module1Name).AddType<DateTime>().As(alias).CompleteType().CompleteModule()
            .DefineModule(module2Name).AddType<TimeSpan>().As(alias).CompleteType().CompleteModule();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        var matchTypes = container.GetMatchingTypes(alias);
        Assert.Equal(2, types.Count);
        Assert.Equal(2, matchTypes.Count);
        
        Assert.Equal(alias, matchTypes[0].Alias);
        Assert.Equal(alias, matchTypes[1].Alias);
        Assert.NotEqual(matchTypes[0].Module, matchTypes[1].Module);
    }

    [Fact]
    public void CreateBaseGenericFromImplementation()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddGenericTypeDefinition<KeyValuePair<object, object>>();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        Assert.Single(types);
        Assert.Equal(typeof(KeyValuePair<,>), types[0].Type);
        Assert.Equal(nameof(KeyValuePair<int, int>), types[0].Alias);
    }

    [Fact]
    public void CreateGenericBaseAndImplementationInSameModule()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<Dictionary<string, object>>().CompleteType()
            .AddGenericTypeDefinition<Dictionary<string, object>>().CompleteType();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        var genericList = container.GetMatchingTypes(nameof(Dictionary<object,object>), 2);
        var def = genericList.Single(x => x.Type.IsGenericTypeDefinition);
        var impl = genericList.Single(x => x.Type.IsConstructedGenericType);
        Assert.Equal(2, types.Count);
        Assert.Equal(2, genericList.Count);
        Assert.Equal(typeof(Dictionary<,>), def.Type);
        Assert.Equal(typeof(Dictionary<string, object>), impl.Type);
    }

    [Fact]
    public void CreateDifferentGenericImplementations()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<Dictionary<int, int>>().CompleteType()
            .AddType<Dictionary<int, object>>();
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        var genericList = container.GetMatchingTypes(nameof(Dictionary<int,string>), 2);
        Assert.Equal(2, types.Count);
        Assert.Equal(2, genericList.Count);
        Assert.Single(genericList, x => x.Type == typeof(Dictionary<int, int>));
        Assert.Single(genericList, x => x.Type == typeof(Dictionary<int, object>));
    }

    [Fact]
    public void CreateGenericDefinitionTwiceInSameModule()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddGenericTypeDefinition<Dictionary<int, string>>().CompleteType()
            .AddGenericTypeDefinition<Dictionary<string, int>>();

        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        Assert.Single(types);
        Assert.Equal(typeof(Dictionary<,>), types[0].Type);
    }

    [Fact]
    public void CreateSameGenericTypeInDifferentModule()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1")
            .AddGenericTypeDefinition<Dictionary<object, object>>().CompleteType().CompleteModule()
            .DefineModule("2");
        Assert.Throws<ArgumentException>(() => syntax.AddGenericTypeDefinition<Dictionary<string, string>>());
    }

    [Fact]
    public void CreateSameTypeWithAlias()
    {
        const string alias = "listOfInt";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<List<int>>().CompleteType()
            .AddType<List<int>>().As(alias);
        var container = builder.CreateContainer();
        var types = container.EnumerateTypes().ToList();
        Assert.Single(types);
        var type = Assert.Single(container.GetMatchingTypes(alias, 1));
        Assert.Equal(alias, type.Alias);
        Assert.Equal(typeof(List<int>), type.Type);
    }
}