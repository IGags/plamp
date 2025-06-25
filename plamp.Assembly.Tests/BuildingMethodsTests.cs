using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class BuildingMethodsTests
{
    [Fact]
    public void AddMethodInvalidExpression()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1").AddType<object>().WithMembers();
        var ls = new List<int>();
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(x => 1 + 2));
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(x => ls.Add(Arg.OfType<int>())));
    }
    
    [Fact]
    public void AddMethodToTypeMethodInfo()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var info = typeof(object).GetMethod(nameof(ToString))!;
        builder.DefineModule("1").AddType<object>().WithMembers().AddMethod(info);
        var container = builder.CreateContainer();
        var type = container.GetMatchingTypes(nameof(Object)).Single();
        var methods = container.GetMatchingMethods(nameof(ToString), type).ToList();
        Assert.Single(methods);
        Assert.Equal(info, methods[0].MethodInfo);
        Assert.Equal(nameof(ToString), methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodToTypeFunc()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<object>().WithMembers().AddMethod(x => x.ToString()!);
        var info = typeof(object).GetMethod(nameof(ToString))!;
        var container = builder.CreateContainer();
        var type = container.GetMatchingTypes(nameof(Object)).Single();
        var methods = container.GetMatchingMethods(nameof(ToString), type).ToList();
        Assert.Single(methods);
        Assert.Equal(info, methods[0].MethodInfo);
        Assert.Equal(nameof(ToString), methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodToTypeAction()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<string>().WithMembers().AddMethod(
            x => x.CopyTo(Arg.OfType<int>(), Arg.OfType<char[]>(), Arg.OfType<int>(), Arg.OfType<int>()));
        
        var info = typeof(string).GetMethod(nameof(string.CopyTo), [typeof(int), typeof(char[]), typeof(int), typeof(int)])!;
        var container = builder.CreateContainer();
        var type = container.GetMatchingTypes(nameof(String)).Single();
        var methods = container.GetMatchingMethods(nameof(string.CopyTo), type).ToList();
        Assert.Single(methods);
        Assert.Equal(info, methods[0].MethodInfo);
        Assert.Equal(nameof(string.CopyTo), methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodToTypeWithAlias()
    {
        const string aliasName = "notToString";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<object>().WithMembers().AddMethod(x => x.ToString()).As(aliasName);

        var info = typeof(object).GetMethod(nameof(ToString));
        var container = builder.CreateContainer();
        var type = container.GetMatchingTypes(nameof(Object)).Single();
        var methods = container.GetMatchingMethods(aliasName, type).ToList();
        Assert.Single(methods);
        Assert.Equal(info, methods[0].MethodInfo);
        Assert.Equal(aliasName, methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodTwiceWithDifferentAlias()
    {
        const string aliasName = "notToString";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1").AddType<object>()
            .WithMembers()
            .AddMethod(x => x.ToString()).As(aliasName)
            .AddMethod(x => x.ToString());

        var info = typeof(object).GetMethod(nameof(ToString));
        var container = builder.CreateContainer();
        var type = container.GetMatchingTypes(nameof(Object)).Single();
        var methods = container.GetMatchingMethods(nameof(ToString), type).ToList();
        Assert.Single(methods);
        Assert.Equal(info, methods[0].MethodInfo);
        Assert.Equal(nameof(ToString), methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodFromGenericTypeImplementation()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<List<int>>().WithMembers()
            .AddMethod(x => x.Add(Arg.OfType<int>()));
        var container = builder.CreateContainer();
        var methodInfo = typeof(List<int>).GetMethod(nameof(List<int>.Add));
        var type = container.GetMatchingTypes(typeof(List<int>).Name).Single();
        var methods = container.GetMatchingMethods(nameof(List<int>.Add), type).ToList();
        Assert.Single(methods);
        Assert.Equal(methodInfo, methods[0].MethodInfo);
        Assert.Equal(nameof(List<int>.Add), methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodFromAnotherType()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1")
            .AddType<object>().WithMembers();
        
        var methodInfo = typeof(IComparable).GetMethod(nameof(IComparable.CompareTo))!;
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(methodInfo));
    }

    [Fact]
    public void AddMethodFromGenericTypeDefinition()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddGenericTypeDefinition<List<int>>().WithMembers().AddMethod(x => x.Add(Arg.OfType<int>()));

        var container = builder.CreateContainer();
        var methodInfo = typeof(List<>).GetMethod(nameof(List<int>.Add));
        var type = container.GetMatchingTypes(typeof(List<int>).Name).Single();
        var methods = container.GetMatchingMethods(nameof(List<int>.Add), type).ToList();
        Assert.Single(methods);
        Assert.Equal(methodInfo, methods[0].MethodInfo);
        Assert.Equal(nameof(List<int>.Add), methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddMethodFromGenericTypeDefinitionWithAlias()
    {
        const string alias = "push";
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddGenericTypeDefinition<List<int>>().WithMembers().AddMethod(x => x.Add(Arg.OfType<int>())).As(alias);
        
        var container = builder.CreateContainer();
        var methodInfo = typeof(List<>).GetMethod(nameof(List<int>.Add));
        var type = container.GetMatchingTypes(typeof(List<int>).Name).Single();
        var methods = container.GetMatchingMethods(alias, type).ToList();
        Assert.Single(methods);
        Assert.Equal(methodInfo, methods[0].MethodInfo);
        Assert.Equal(alias, methods[0].Alias);
        Assert.Equal(type, methods[0].EnclosingType);
    }

    [Fact]
    public void AddSameMethodFromDefinitionAndImplementation()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<List<int>>().WithMembers().AddMethod(x => x.Add(Arg.OfType<int>())).CompleteType()
            .AddGenericTypeDefinition<List<int>>().WithMembers().AddMethod(x => x.Add(Arg.OfType<int>()));

        var container = builder.CreateContainer();
        var types = container.GetMatchingTypes(typeof(List<int>).Name);

        var generalInfo = typeof(List<>).GetMethod(nameof(List<int>.Add));
        var implementationInfo = typeof(List<int>).GetMethod(nameof(List<int>.Add));
        
        var def = Assert.Single(types, x => x.Type == typeof(List<>))!;
        var impl = Assert.Single(types, x => x.Type == typeof(List<int>))!;

        var defMethods = container.GetMatchingMethods(nameof(List<int>.Add), def);
        var implMethods = container.GetMatchingMethods(nameof(List<int>.Add), impl);

        var defMethod = Assert.Single(defMethods);
        var implMethod = Assert.Single(implMethods);
        
        Assert.Equal(generalInfo, defMethod.MethodInfo);
        Assert.Equal(generalInfo!.Name, defMethod.Alias);
        Assert.Equal(def, defMethod.EnclosingType);
        
        Assert.Equal(implementationInfo, implMethod.MethodInfo);
        Assert.Equal(implementationInfo!.Name, implMethod.Alias);
        Assert.Equal(impl, implMethod.EnclosingType);
    }

    [Fact]
    public void AddSeveralOverloads()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<string>().WithMembers()
            .AddMethod(x => x.Trim())
            .AddMethod(x => x.Trim(Arg.OfType<char>()));

        var container = builder.CreateContainer();

        var types = container.GetMatchingTypes(nameof(String));
        var type = Assert.Single(types);
        var methods = container.GetMatchingMethods(nameof(string.Trim), type);

        var emptyOverload = typeof(string).GetMethod(nameof(string.Trim), []);
        var charOverload = typeof(string).GetMethod(nameof(string.Trim), [typeof(char)]);
        
        Assert.Equal(2, methods.Count);
        var emptyInfo = Assert.Single(methods, x => x.MethodInfo == emptyOverload);
        var charInfo = Assert.Single(methods, x => x.MethodInfo == charOverload);
        
        Assert.Equal(type, emptyInfo.EnclosingType);
        Assert.Equal(nameof(string.Trim), emptyInfo.Alias);
        
        Assert.Equal(type, charInfo.EnclosingType);
        Assert.Equal(nameof(string.Trim), charInfo.Alias);
    }

    [Fact]
    public void AddGenericMethodInNonGenericType()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<ExampleNonGeneric>().WithMembers().AddMethod(x => x.ExampleGenericMth(Arg.OfType<int>()));

        var container = builder.CreateContainer();

        var methodInfo = typeof(ExampleNonGeneric).GetMethod(nameof(ExampleNonGeneric.ExampleGenericMth));
        var types = container.GetMatchingTypes(nameof(ExampleNonGeneric));
        var type = Assert.Single(types);
        var methods = container.GetMatchingMethods(nameof(ExampleNonGeneric.ExampleGenericMth), type);
        var method = Assert.Single(methods);
        Assert.Equal(methodInfo, method.MethodInfo);
        Assert.Equal(nameof(ExampleNonGeneric.ExampleGenericMth), method.Alias);
        Assert.Equal(type, method.EnclosingType);
    }

    [Fact]
    public void AddGenericMethodInGenericTypeImpl()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<ExampleGeneric<int>>().WithMembers().AddMethod(x => x.ExampleGenericMth(Arg.OfType<int>()));
        
        var container = builder.CreateContainer();

        var methodInfo = typeof(ExampleGeneric<int>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == 1 && x.GetGenericArguments().Length == 1);
        var types = container.GetMatchingTypes(typeof(ExampleGeneric<>).Name);
        var type = Assert.Single(types);
        var methods = container.GetMatchingMethods(nameof(ExampleGeneric<int>.ExampleGenericMth), type);
        var method = Assert.Single(methods);
        Assert.Equal(methodInfo, method.MethodInfo);
        Assert.Equal(nameof(ExampleGeneric<int>.ExampleGenericMth), method.Alias);
        Assert.Equal(type, method.EnclosingType);
    }

    [Fact]
    public void AddGenericMethodInGenericTypeWithTypeGeneric()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<ExampleGeneric<int>>().WithMembers().AddMethod(x => x.ExampleGenericMth(Arg.OfType<int>(), Arg.OfType<int>()));
        
        var container = builder.CreateContainer();

        var methodInfo = typeof(ExampleGeneric<int>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == 2 && x.GetGenericArguments().Length == 1);
        var types = container.GetMatchingTypes(typeof(ExampleGeneric<>).Name);
        var type = Assert.Single(types);
        var methods = container.GetMatchingMethods(nameof(ExampleGeneric<int>.ExampleGenericMth), type);
        var method = Assert.Single(methods);
        Assert.Equal(methodInfo, method.MethodInfo);
        Assert.Equal(nameof(ExampleGeneric<int>.ExampleGenericMth), method.Alias);
        Assert.Equal(type, method.EnclosingType);
    }

    [Fact]
    public void AddGenericMethodInGenericDefinitionWithGenericArg()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddGenericTypeDefinition<ExampleGeneric<int>>().WithMembers()
            .AddMethod(x => x.ExampleGenericMth(Arg.OfType<int>()));

        var container = builder.CreateContainer();
        
        var methodInfo = typeof(ExampleGeneric<>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == 1 && x.GetGenericArguments().Length == 1);
        var types = container.GetMatchingTypes(typeof(ExampleGeneric<>).Name);
        var type = Assert.Single(types);
        var methods = container.GetMatchingMethods(nameof(ExampleGeneric<int>.ExampleGenericMth), type);
        var method = Assert.Single(methods);
        Assert.Equal(methodInfo, method.MethodInfo);
        Assert.Equal(nameof(ExampleGeneric<int>.ExampleGenericMth), method.Alias);
        Assert.Equal(type, method.EnclosingType);
    }

    [Fact]
    public void AddGenericMethodInGenericDefinitionWithoutGenericArg()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddGenericTypeDefinition<ExampleGeneric<int>>().WithMembers()
            .AddMethod(x => x.ExampleGenericMth(Arg.OfType<int>(), Arg.OfType<object>()));

        var container = builder.CreateContainer();
        
        var methodInfo = typeof(ExampleGeneric<>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .First(x => x.GetParameters().Length == 2 && x.GetGenericArguments().Length == 1);
        var types = container.GetMatchingTypes(typeof(ExampleGeneric<>).Name);
        var type = Assert.Single(types);
        var methods = container.GetMatchingMethods(nameof(ExampleGeneric<int>.ExampleGenericMth), type);
        var method = Assert.Single(methods);
        Assert.Equal(methodInfo, method.MethodInfo);
        Assert.Equal(nameof(ExampleGeneric<int>.ExampleGenericMth), method.Alias);
        Assert.Equal(type, method.EnclosingType);
    }

    [Fact]
    public void AddMethodWithMatchingSignatureAndAlias()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1")
            .AddType<ExampleMethodMatch>().WithMembers()
                .AddMethod(x => x.Method1(Arg.OfType<int>()))
                .As(nameof(ExampleMethodMatch.Method2));
        Assert.Throws<ArgumentException>(() => syntax.AddMethod(x => x.Method2(Arg.OfType<int>())));
    }

    [Fact]
    public void AddMethodWithMatchingAliasSecond()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        var syntax = builder.DefineModule("1")
            .AddType<ExampleMethodMatch>().WithMembers()
            .AddMethod(x => x.Method1(Arg.OfType<int>()))
            .AddMethod(x => x.Method2(Arg.OfType<int>()));
        Assert.Throws<ArgumentException>(() => syntax.As(nameof(ExampleMethodMatch.Method1)));
    }

    [Fact]
    public void AddSameMethodInTwoSameTypes()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<string>().WithMembers().AddMethod(x => x.ToString()).CompleteType()
            .AddType<string>().WithMembers().AddMethod(x => x.ToString()).CompleteType();
        var container = builder.CreateContainer();
        var type = Assert.Single(container.EnumerateTypes());
        var methods = container.GetMatchingMethods(nameof(ToString), type);
        Assert.Single(methods);
    }

    [Fact]
    public void AddMethodOverrideFromBase()
    {
        var builder = NativeAssemblyContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("1")
            .AddType<ToStringOverride>().WithMembers().AddMethod(x => x.ToString());
        var container = builder.CreateContainer();
        var type = Assert.Single(container.EnumerateTypes());
        var methods = container.GetMatchingMethods(nameof(ToString), type);
        var method = Assert.Single(methods);
        Assert.Equal(typeof(object), method.MethodInfo.DeclaringType);
    }
}

public class ToStringOverride
{
    public override string ToString() => "TOTOTOTOTTO";
}

public class ExampleMethodMatch
{
    public void Method1(int a) {}
    
    public void Method2(int a) {}
}

public class ExampleNonGeneric
{
    public void ExampleGenericMth<T>(T arg){}
}

public class ExampleGeneric<T>
{
    public void ExampleGenericMth<T2>(T2 arg){}
    
    public void ExampleGenericMth<T2>(T arg1, T2 arg2) {}
}