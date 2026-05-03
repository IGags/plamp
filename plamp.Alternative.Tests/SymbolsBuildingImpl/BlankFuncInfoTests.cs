using System;
using System.Reflection;
using System.Reflection.Emit;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.SymbolsBuildingImpl;

public class BlankFuncInfoTests
{
    [Fact]
    public void SimpleFuncName_ReturnsSameNameAsDefined()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        info.Name.ShouldBe("fff()");
        info.DefinitionName.ShouldBe("fff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(false);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.ShouldBeEmpty();
        info.ModuleName.ShouldBe("test");
    }

    [Fact]
    public void FuncNonNullReturnType_TypeDoesNotAppear()
    {
        var info = new BlankFuncInfo("ff", [], Builtins.Int, "test");
        info.Name.ShouldBe("ff()");
        info.DefinitionName.ShouldBe("ff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(false);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.ShouldBeEmpty();
        info.ModuleName.ShouldBe("test");
    }

    [Fact]
    public void FuncWithArgs_ReturnsInParensCommaSeparated()
    {
        var info = new BlankFuncInfo("ff", 
            [new BlankArgInfo("f", Builtins.Int), new BlankArgInfo("s", Builtins.String)], 
            Builtins.Void, "test");
        info.Name.ShouldBe($"ff({Builtins.Int.Name}, {Builtins.String.Name})");
        info.DefinitionName.ShouldBe("ff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(false);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.Count.ShouldBe(2);
        info.ModuleName.ShouldBe("test");
    }

    [Fact]
    public void FuncWithGenericDefs_ReturnsParamNamesInSquareBraces()
    {
        var info = new BlankFuncInfo("ff", [], Builtins.Void,
            [new GenericParameterBuilder("T", "test"), new GenericParameterBuilder("T2", "test")], "test");
        info.Name.ShouldBe("ff[T, T2]()");
        info.DefinitionName.ShouldBe("ff");
        info.IsGenericFunc.ShouldBe(false);
        info.IsGenericFuncDefinition.ShouldBe(true);
        info.GetGenericArguments().ShouldBeEmpty();
        info.GetGenericParameters().ShouldNotBeEmpty();
        info.GetGenericFuncDefinition().ShouldBeNull();
        info.Arguments.ShouldBeEmpty();
        info.ModuleName.ShouldBe("test");
    }

    [Fact]
    public void CreateInfoWithDuplicateGenericParams_Throws()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFuncInfo("ff", [], Builtins.Void,
            [new GenericParameterBuilder("T", "test"), new GenericParameterBuilder("T", "test")], "test"));
    }

    [Fact]
    public void CreateWithGenericParamFromOtherModule_Throws()
    {
        Should.Throw<InvalidOperationException>(() => new BlankFuncInfo("ff", [], Builtins.Void,
            [new GenericParameterBuilder("T", "test2")], "test"));
    }

    [Fact]
    public void AsFuncEmpty_Throws()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        Should.Throw<NullReferenceException>(() => info.AsFunc());
    }

    [Fact]
    public void AsFuncWithBuilder_Correct()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("123"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("421");
        var method = mod.DefineGlobalMethod("214", MethodAttributes.Static, CallingConventions.Standard, null, []);
        info.MethodBuilder = method;
        var res = info.AsFunc();
        res.ShouldBe(method);
    }

    [Fact]
    public void MakeGenericFuncFromNonGeneric_ReturnsNull()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        var res = info.MakeGenericFunc([Builtins.Int]);
        res.ShouldBeNull();
    }

    [Fact]
    public void MakeGenericFuncFromGenericDef_Correct()
    {
        var info = new BlankFuncInfo("ff", [], Builtins.Void, [new GenericParameterBuilder("T", "test")], "test");
        var genInfo = info.MakeGenericFunc([Builtins.Int]);
        genInfo.ShouldNotBeNull();
        genInfo.ShouldNotBe(info);
    }

    [Fact]
    public void EqualityDoesNotDependOnBuilder_Correct()
    {
        var info = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        var asm = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("123"), AssemblyBuilderAccess.RunAndCollect);
        var mod = asm.DefineDynamicModule("421");
        var method = mod.DefineGlobalMethod("214", MethodAttributes.Static, CallingConventions.Standard, null, []);
        info.MethodBuilder = method;
        
        var info2 = new BlankFuncInfo("fff", [], Builtins.Void, "test");
        info.ShouldBe(info2);
    }
}