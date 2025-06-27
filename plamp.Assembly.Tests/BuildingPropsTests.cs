using System;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingPropsTests
{
    [Fact]
    public void AddPropertyInfo()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var propertyInfo = typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.SimpleProperty))!;
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers().AddProperty(propertyInfo);
        var container = builder.CreateContainer();

        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.SimpleProperty), type);
        var property = Assert.Single(properties);
        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.SimpleProperty), property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddPropertyExpression()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleProperty);
        var container = builder.CreateContainer();
        
        var propertyInfo = typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.SimpleProperty))!;
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.SimpleProperty), type);
        var property = Assert.Single(properties);
        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.SimpleProperty), property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddPropertyAlias()
    {
        const string alias = "notSimpleProperty";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleProperty).As(alias);

        var container = builder.CreateContainer();
        var propertyInfo = typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.SimpleProperty))!;
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(alias, type);
        var property = Assert.Single(properties);
        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(alias, property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddPropertyTwiceRemoveAlias()
    {
        const string alias = "sureNotSimple";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleProperty).As(alias)
            .AddPropertyOrField(x => x.SimpleProperty);

        var container = builder.CreateContainer();
        var propertyInfo = typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.SimpleProperty))!;
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.SimpleProperty), type);
        var property = Assert.Single(properties);
        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.SimpleProperty), property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddPropertyTwiceAddAlias()
    {
        const string alias = "sureNotSimple";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleProperty)
            .AddPropertyOrField(x => x.SimpleProperty).As(alias);

        var container = builder.CreateContainer();
        var propertyInfo = typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.SimpleProperty))!;
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(alias, type);
        var property = Assert.Single(properties);
        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(alias, property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddGenericPropertyImpl()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.GenericProperty);

        var container = builder.CreateContainer();
        var propertyInfo = typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.GenericProperty))!;
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.GenericProperty), type);
        var property = Assert.Single(properties);
        
        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.GenericProperty), property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddGenericPropertyDef()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddGenericTypeDefinition<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.GenericProperty);
        
        var container = builder.CreateContainer();
        var propertyInfo = typeof(ExamplePropClass<int>).GetGenericTypeDefinition().GetProperty(nameof(ExamplePropClass<int>.GenericProperty))!;
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);
        var properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.GenericProperty), type);
        var property = Assert.Single(properties);

        Assert.Equal(type, property.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.GenericProperty), property.Alias);
        Assert.Equal(propertyInfo, property.PropertyInfo);
    }

    [Fact]
    public void AddTwoProperties()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleProperty)
            .AddPropertyOrField(x => x.GenericProperty);

        var container = builder.CreateContainer();
        var simplePropertyInfo =
            typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.SimpleProperty));
        var genericPropertyInfo =
            typeof(ExamplePropClass<int>).GetProperty(nameof(ExamplePropClass<int>.GenericProperty));
        
        var types = container.GetMatchingTypes(nameof(ExamplePropClass<int>), 1);
        var type = Assert.Single(types);

        var properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.SimpleProperty), type);
        var simpleProperty = Assert.Single(properties);
        properties = container.GetMatchingProperties(nameof(ExamplePropClass<int>.GenericProperty), type);
        var genericProperty = Assert.Single(properties);
        
        Assert.Equal(type, simpleProperty.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.SimpleProperty), simpleProperty.Alias);
        Assert.Equal(simplePropertyInfo, simpleProperty.PropertyInfo);
        
        Assert.Equal(type, genericProperty.EnclosingType);
        Assert.Equal(nameof(ExamplePropClass<int>.GenericProperty), genericProperty.Alias);
        Assert.Equal(genericPropertyInfo, genericProperty.PropertyInfo);
    }

    [Fact]
    public void AddPropertyWithCollisionName()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<ExamplePropClass<int>>().WithMembers()
            .AddPropertyOrField(x => x.SimpleProperty).As(nameof(ExamplePropClass<int>.GenericProperty));

        Assert.Throws<ArgumentException>(() => syntax.AddPropertyOrField(x => x.GenericProperty));
    }
}

public class ExamplePropClass<T>
{
    public int SimpleProperty { get; set; }
    
    public T GenericProperty { get; set; } = default!;
    
    private int _initProperty;
    
    //Set property isn't supported as expression
    public int InitProperty
    {
        set => _initProperty = value;
    }
}