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
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleStructCtor>().WithMembers().AddCtor(() => new());
        var constructorInfo = typeof(ExampleStructCtor).GetConstructors().SingleOrDefault(x => x.GetParameters().Length == 0);
        
        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(nameof(ExampleStructCtor));
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(constructorInfo, ctor.ConstructorInfo);
    }

    [Fact]
    public void AddStructCtorWithArgs()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleStructCtor>().WithMembers().AddCtor(() => new(Arg.OfType<int>()));
        var constructorInfo = typeof(ExampleStructCtor).GetConstructors().SingleOrDefault(x => x.GetParameters().Length == 1);
        
        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(nameof(ExampleStructCtor));
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(constructorInfo, ctor.ConstructorInfo);
    }
    
    [Fact]
    public void AddDefaultCtorInfo()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
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
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
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
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
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
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
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

    [Fact]
    public void AddGenericDefCtor()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<ExampleGenericCtor<int>>().WithMembers()
            .AddCtor(() => new ExampleGenericCtor<int>(Arg.OfType<int>()));
        
        var container = builder.CreateContainer();
        var ctorInfo = typeof(ExampleGenericCtor<int>).GetGenericTypeDefinition().GetConstructors().Single();
        var types = container.GetMatchingTypes(nameof(ExampleGenericCtor<int>), 1);
        var type = Assert.Single(types);
        var constructors = container.GetMatchingConstructors(type);
        var ctor = Assert.Single(constructors);
        Assert.Equal(type, ctor.EnclosingType);
        Assert.Equal(ctorInfo, ctor.ConstructorInfo);
    }
}

public class ExampleGenericCtor<T>
{
    
    public ExampleGenericCtor(T a) {}
}

public struct ExampleStructCtor
{
    public ExampleStructCtor() { }
    
    public ExampleStructCtor(int a) { }
}