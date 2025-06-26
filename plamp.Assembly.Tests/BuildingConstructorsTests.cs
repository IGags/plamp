using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingConstructorsTests
{
    [Fact]
    public void AddDefaultStructCtor()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<KeyValuePair<int, int>>().WithMembers().AddCtor(() => new());
        var constructorInfo = typeof(KeyValuePair<int, int>).GetConstructors().Single();
        
        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(typeof(KeyValuePair<int, int>).Name);
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(constructorInfo, ctor.ConstructorInfo);
    }
    
    [Fact]
    public void AddDefaultCtorInfo()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var constructorInfo = typeof(string).GetConstructor([typeof(char), typeof(int)])!;
        builder.DefineModule("1").AddType<string>().WithMembers().AddCtor(constructorInfo);

        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(nameof(String));
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(constructorInfo, ctor.ConstructorInfo);
    }

    [Fact]
    public void AddDefaultCtorExpression()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<string>().WithMembers()
            .AddCtor(() => new string(Arg.OfType<char>(), Arg.OfType<int>()));
        var container = builder.CreateContainer();
        
        var constructorInfo = typeof(string).GetConstructor([typeof(char), typeof(int)])!;
        var types = container.GetMatchingTypes(nameof(String));
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(constructorInfo, ctor.ConstructorInfo);
    }

    [Fact]
    public void AddCtorTwice()
    {
        var constructorInfo = typeof(string).GetConstructor([typeof(char), typeof(int)])!;
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<string>().WithMembers()
            .AddCtor(() => new string(Arg.OfType<char>(), Arg.OfType<int>()))
            .AddCtor(constructorInfo);
        
        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(nameof(String));
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(constructorInfo, ctor.ConstructorInfo);
    }

    [Fact]
    public void AddTwoDifferentCtors()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<string>().WithMembers()
            .AddCtor(() => new string(Arg.OfType<char>(), Arg.OfType<int>()))
            .AddCtor(() => new string(Arg.OfType<char[]>()));
        
        var charCtor = typeof(string).GetConstructor([typeof(char), typeof(int)])!;
        var arrayCtor = typeof(string).GetConstructor([typeof(char[])])!;
        
        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(nameof(String));
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        Assert.Equal(2, constructors.Count);
        var charCtorInfo = Assert.Single(constructors, x => x.ConstructorInfo == charCtor);
        var arrayCtorInfo = Assert.Single(constructors, x => x.ConstructorInfo == arrayCtor);
        Assert.Equal(type, charCtorInfo.EnclosingType);
        Assert.Equal(type, arrayCtorInfo.EnclosingType);
    }
    
    // public void AddGener
}

public class ExampleGenericCtor<T>
{
    public ExampleGenericCtor(int a) { }
    
    public ExampleGenericCtor(T a) {}
}