using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Assemblies;
using plamp.Assembly.Building;
using Xunit;

namespace plamp.Assembly.Tests;

public class ContainerMethodMatchingTests
{
    private readonly IAssemblyContainer _container;
    private readonly ITypeInfo _typeInfo;
    
    public ContainerMethodMatchingTests()
    {
        var builder = ScriptedContainerBuilder.CreateContainerBuilder();
        builder.DefineModule("default").AddType<ExampleMethodSignatureMatchClass<int>>()
            .WithMembers()
            .AddMethod(x => x.StrictSignature(Arg.OfType<int>(), Arg.OfType<int>()))
            .AddMethod(x => x.OptionalArgs(Arg.OfType<int>(), Arg.OfType<string>(), Arg.OfType<int>()))
            .AddMethod(x => x.InterfaceArg(Arg.OfType<IComparable>()))
            .AddMethod(x => x.GenericMethodWithoutRestrictions(Arg.OfType<object>()))
            .AddMethod(x => x.GenericMethodWithInterfaceRestriction(Arg.OfType<IComparable>()))
            .AddMethod(x => x.GenericMethodWithNewRestriction(Arg.OfType<object>()))
            .AddMethod(x => x.GenericMethodWithStructRestriction(Arg.OfType<ValueTuple>()))
            .AddMethod(x => x.GenericMethodWithClassRestriction(Arg.OfType<object>()))
            .AddMethod(x => x.GenericMethodWithManyInterfaceRestrictions(Arg.OfType<string>()))
            .AddMethod(x => x.GenericMethod(Arg.OfType<int>()));
        _container = builder.CreateContainer();
        _typeInfo = _container.GetMatchingTypes(nameof(ExampleMethodSignatureMatchClass<int>), 1).Single();
    }

    [Theory]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.StrictSignature), new[]{typeof(int), typeof(int)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.OptionalArgs), new[]{typeof(int), typeof(string), typeof(int)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.OptionalArgs), new[]{typeof(int)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.OptionalArgs), new[]{typeof(int), null})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.OptionalArgs), new[]{typeof(int), null, typeof(int)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.OptionalArgs), new[]{typeof(int), typeof(string)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithoutRestrictions), new[]{typeof(object)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithoutRestrictions), new[]{typeof(ValueTuple)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithoutRestrictions), new[]{typeof(IComparable)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithInterfaceRestriction), new[]{typeof(IComparable)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithInterfaceRestriction), new[]{typeof(int)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithNewRestriction), new[]{typeof(object)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithNewRestriction), new[]{typeof(KeyValuePair<int,int>)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithStructRestriction), new[]{typeof(ValueTuple)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithStructRestriction), new[]{typeof(int)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithStructRestriction), new[]{typeof(DateTime)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithClassRestriction), new[]{typeof(object)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithClassRestriction), new[]{typeof(string)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithClassRestriction), new[]{typeof(int[])})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithClassRestriction), new[]{typeof(List<>)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethodWithManyInterfaceRestrictions), new[]{typeof(string)})]
    
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethod), new[]{typeof(object)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethod), new[]{typeof(int)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethod), new[]{typeof(ValueTuple)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethod), new[]{typeof(string)})]
    [InlineData(nameof(ExampleMethodSignatureMatchClass<int>.GenericMethod), new[]{typeof(int[])})]
    public void Match(string methodName, Type?[] signature, bool fault = false)
    {
        var methodInfo = _typeInfo.Type.GetMethod(methodName);
        var args = signature.Select(x => x == null ? null : new ParameterImpl(x)).ToList();
        var methods = _container.GetMatchingMethods(methodName, _typeInfo, args);
        
        if (!fault)
        {
            var method = Assert.Single(methods);
            Assert.Equal(methodInfo, method.MethodInfo);
            Assert.Equal(methodName, method.Alias);
            Assert.Equal(_typeInfo, method.EnclosingType);
        }
        else
        {
            Assert.Empty(methods);
        }
    }
    
    private class ParameterImpl : ParameterInfo
    {
        public override Type ParameterType { get; }

        public ParameterImpl(Type type)
        {
            ParameterType = type;
        }
    }
}

public class ExampleMethodSignatureMatchClass<T>
{
    public void StrictSignature(int a, int b) {}

    public void OptionalArgs(int a, string? b = null, int c = 0) {}
    
    public void InterfaceArg(IComparable a) {}
    
    public void GenericMethodWithoutRestrictions<T2>(T2 a){}

    public void GenericMethodWithInterfaceRestriction<T2>(T2 a) where T2 : IComparable {}
    
    public void GenericMethodWithNewRestriction<T2>(T2 a) where T2 : new() {}
    
    public void GenericMethodWithStructRestriction<T2>(T2 a) where T2 : struct {}
    
    public void GenericMethodWithClassRestriction<T2>(T2 a) where T2 : class {}

    public void GenericMethodWithManyInterfaceRestrictions<T2>(T2 a) where T2 : IComparable, ICloneable {}

    public void GenericMethod(T arg) {}
}