using System;
using System.Linq;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingIndexerTests
{
    [Fact]
    public void AddIndexerPropertyInfo()
    {
        var propertyInfo = typeof(ExampleIndexerClass<string>)
            .GetProperties()
            .Single(x =>
                x.GetIndexParameters().Length == 1 && x.GetIndexParameters()[0].ParameterType == typeof(string));
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleIndexerClass<string>>().WithMembers()
            .AddIndexer(propertyInfo);
        var container = builder.CreateContainer();
        
        var type = Assert.Single(container.GetMatchingTypes(nameof(ExampleIndexerClass<string>), 1));
        var indexers = container.GetMatchingIndexers(type);
        var indexer = Assert.Single(indexers);
        Assert.Equal(propertyInfo, indexer.IndexerProperty);
        Assert.Equal(type, indexer.EnclosingType);
    }

    [Fact]
    public void AddIndexerExpression()
    {
        var propertyInfo = typeof(ExampleIndexerClass<int>)
            .GetProperties()
            .Single(x =>
                x.GetIndexParameters().Length == 1 && x.GetIndexParameters()[0].ParameterType == typeof(int));
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleIndexerClass<int>>().WithMembers()
            .AddIndexer(x => x[Arg.OfType<int>()]);
        var container = builder.CreateContainer();
        var type = Assert.Single(container.GetMatchingTypes(nameof(ExampleIndexerClass<int>), 1));
        var indexers = container.GetMatchingIndexers(type);
        var indexer = Assert.Single(indexers);
        Assert.Equal(propertyInfo, indexer.IndexerProperty);
        Assert.Equal(type, indexer.EnclosingType);
    }

    [Fact]
    public void AddIndexerTwice()
    {
        var propertyInfo = typeof(ExampleIndexerClass<int>)
            .GetProperties()
            .Single(x =>
                x.GetIndexParameters().Length == 1 && x.GetIndexParameters()[0].ParameterType == typeof(int));
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleIndexerClass<int>>().WithMembers()
            .AddIndexer(x => x[Arg.OfType<int>()])
            .AddIndexer(x => x[Arg.OfType<int>()]);
        ;
        var container = builder.CreateContainer();
        var type = Assert.Single(container.GetMatchingTypes(nameof(ExampleIndexerClass<int>), 1));
        var indexers = container.GetMatchingIndexers(type);
        var indexer = Assert.Single(indexers);
        Assert.Equal(propertyInfo, indexer.IndexerProperty);
        Assert.Equal(type, indexer.EnclosingType);
    }

    [Fact]
    public void AddGenericIndexerDef()
    {
        var propertyInfo = typeof(ExampleIndexerClass<>).GetProperties()
            .Single(x => x.GetIndexParameters().Length == 1);
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddGenericTypeDefinition<ExampleIndexerClass<int>>().WithMembers()
            .AddIndexer(x => x[Arg.OfType<int>()]);
        var container = builder.CreateContainer();
        var type = Assert.Single(container.GetMatchingTypes(nameof(ExampleIndexerClass<int>), 1));
        var indexers = container.GetMatchingIndexers(type);
        var indexer = Assert.Single(indexers);
        Assert.Equal(propertyInfo, indexer.IndexerProperty);
        Assert.Equal(type, indexer.EnclosingType);
    }

    [Fact]
    public void AddNonIndexerPropertyInfo()
    {
        var propertyInfo = typeof(ExampleIndexerClass<>).GetProperty(nameof(ExampleIndexerClass<int>.QuiteNotIndexer))!;
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleIndexerClass<int>>().WithMembers();
        Assert.Throws<ArgumentException>(() => syntax.AddIndexer(propertyInfo));
    }

    [Fact]
    public void AddNonIndexerPropertyExpression()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExampleIndexerClass<int>>().WithMembers();
        Assert.Throws<ArgumentException>(() => syntax.AddIndexer(x => x.QuiteNotIndexer));
    }
}

public class ExampleIndexerClass<T>
{
    public int QuiteNotIndexer { get; } = 1;
    
    public T? this[T index]
    {
        get => default;
        set => throw new NotImplementedException();
    }
}