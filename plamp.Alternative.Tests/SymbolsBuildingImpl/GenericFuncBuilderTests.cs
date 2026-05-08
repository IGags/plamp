using System;
using System.Linq;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class GenericFuncBuilderTests
{
    [Fact]
    public void CreateBuilderBaseIsNotGenericFuncDef_Throws()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, "modue");

        Should.Throw<InvalidOperationException>(() => new GenericFuncBuilder(baseBuilder, [Builtins.Int]));
    }

    [Fact]
    public void CreateBuilderArgumentIsGenericDef_Throws()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");

        var genericTypeDef = new TypeBuilder("TYP", [new GenericParameterBuilder("T", "module")], "module");
        Should.Throw<InvalidOperationException>(() => new GenericFuncBuilder(baseBuilder, [genericTypeDef]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CreateBuilderWithMismatchArgumentCount_Throws(int count)
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");
        var args = Enumerable.Repeat(Builtins.Int, count);
        Should.Throw<InvalidOperationException>(() => new GenericFuncBuilder(baseBuilder, args.ToArray()));
    }

    [Fact]
    public void CreateBuilderVoidAsArgument_Throws()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");
        Should.Throw<InvalidOperationException>(() => new GenericFuncBuilder(baseBuilder, [Builtins.Void]));
    }

    [Fact]
    public void InfoWithoutFunc_Throws()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");
        var impl = new GenericFuncBuilder(baseBuilder, [Builtins.Int]);
        Should.Throw<NullReferenceException>(() => impl.AsFunc());
    }

    [Fact]
    public void NameShouldReturnsGenericArgTypesInSquareBraces_Correct()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [
            new GenericParameterBuilder("T", "module"),
            new GenericParameterBuilder("T2", "module")
        ], "module");

        var impl = new GenericFuncBuilder(baseBuilder, [Builtins.Int, Builtins.String]);
        impl.Name.ShouldBe("fff[int, string]()");
        impl.DefinitionName.ShouldBe("fff");
    }

    [Fact]
    public void GetDefinitionReturnsBaseFunc_Correct()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");
        var impl = new GenericFuncBuilder(baseBuilder, [Builtins.Int]);
        
        impl.GetGenericFuncDefinition().ShouldBe(baseBuilder);
    }

    [Fact]
    public void IsGenericFuncShouldBeTrueNeitherGenericFuncDef_Correct()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");
        var impl = new GenericFuncBuilder(baseBuilder, [Builtins.Int]);
        
        impl.IsGenericFunc.ShouldBeTrue();
        impl.IsGenericFuncDefinition.ShouldBeFalse();
    }

    [Fact]
    public void ImplementFuncReplaceMatchingGenericParams_Correct()
    {
        var genericParamType1 = new GenericParameterBuilder("T", "test");
        var genericPAramType2 = new GenericParameterBuilder("T2", "test");
        var genericParamUsingType = new TypeBuilder("Map",
            [new GenericParameterBuilder("TKey", "test"), new GenericParameterBuilder("TValue", "test")], "test");

        var genericImpl = genericParamUsingType.MakeGenericType([genericParamType1, genericPAramType2])
            .ShouldNotBeNull().MakeArrayType().ShouldNotBeNull();
        
        var baseBuilder = new BlankFuncInfo("fff",
            [
                new BlankArgInfo("first", genericImpl),
                new BlankArgInfo("second", Builtins.Int)
            ], Builtins.Void,
            [
                genericParamType1,
                genericPAramType2
            ], "test");

        var impl = baseBuilder.MakeGenericFunc([Builtins.String, Builtins.Int]);
        impl.ShouldNotBeNull();
        var genericArgs = impl.GetGenericArguments();
        impl.GetGenericParameters().ShouldBeEmpty();
        genericArgs.Count.ShouldBe(2);
        genericArgs[0].ShouldBe(Builtins.String);
        genericArgs[1].ShouldBe(Builtins.Int);

        var args = impl.Arguments;
        args.Count.ShouldBe(2);
        var firstTypeShould = genericParamUsingType.MakeGenericType([Builtins.String, Builtins.Int])
            .ShouldNotBeNull().MakeArrayType().ShouldNotBeNull();
        args[0].Type.ShouldBe(firstTypeShould);
        args[1].Type.ShouldBe(Builtins.Int);
    }
    
    [Fact]
    public void ImplementFuncReplaceReturnType_Correct()
    {
        var genericParamType1 = new GenericParameterBuilder("T", "test");
        var baseBuilder = new BlankFuncInfo("ff", [], genericParamType1, [genericParamType1], "test");
        var impl = baseBuilder.MakeGenericFunc([Builtins.Any.MakeArrayType()]);
        impl.ShouldNotBeNull();
        impl.GetGenericParameters().ShouldBeEmpty();
        impl.GetGenericArguments().ShouldHaveSingleItem().ShouldBe(Builtins.Any.MakeArrayType());
        impl.ReturnType.ShouldBe(Builtins.Any.MakeArrayType());
    }

    [Fact]
    public void EqualityDoesDependsOnNameAndModuleOnly_Correct()
    {
        var baseBuilder = new BlankFuncInfo("fff", [], Builtins.Void, [new GenericParameterBuilder("T", "module")], "module");
        var impl = new GenericFuncBuilder(baseBuilder, [Builtins.Int]);
        var impl2 = new GenericFuncBuilder(baseBuilder, [Builtins.Int]);
        impl.ShouldBe(impl2);
    }
}