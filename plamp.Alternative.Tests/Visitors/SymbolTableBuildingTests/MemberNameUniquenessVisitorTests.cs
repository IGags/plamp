using System.Linq;
using plamp.Alternative.Visitors.SymbolTableBuilding;
using plamp.Alternative.Visitors.SymbolTableBuilding.MemberNameUniqueness;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.SymbolTableBuildingTests;

public class MemberNameUniquenessVisitorTests
{
    //Тип и функция, одинаковые имена - корректно 

    [Fact]
    //Пустой модуль - корректно
    public void EmptyModule_Correct()
    {
        var code = "module test;";
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    //Тип и функция, разные имена - корректно
    public void TypeAndFuncNamesDiffers_Correct()
    {
        var code = """
                   module test;
                   fn A() {}
                   data B {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void SameTypeAndFuncName_ReturnsException()
    {
        var code = """
                   module test;
                   fn A() {}
                   data A {}
                   """;
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.Select(x => x.Code).All(x => x == PlampExceptionInfo.DuplicateMemberNameInModule().Code)
            .ShouldBeTrue();
    }

    [Fact]
    public void DuplicateFuncName_ReturnsException()
    {
        var code = """
                   module test;
                   fn A(f: int) {}
                   fn A() {}
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.Select(x => x.Code).All(x => x == PlampExceptionInfo.DuplicateMemberNameInModule().Code)
            .ShouldBeTrue();
    }

    [Fact]
    public void DuplicateTypeName_ReturnsException()
    {
        var code = """
                   module test;
                   data A;
                   data A[B];
                   """;
        
        var res = SetupAndAct(code);
        res.Exceptions.Count.ShouldBe(2);
        res.Exceptions.Select(x => x.Code).All(x => x == PlampExceptionInfo.DuplicateMemberNameInModule().Code)
            .ShouldBeTrue();
    }
    
    private SymbolTableBuildingContext SetupAndAct(string code)
    {
        var weaver = new MemberNameUniquenessValidator();
        var (ctx, _) =  CompilationPipelineBuilder.RunSymTableVisitors(
            code,
            [(ast, ctx) => weaver.Validate(ast, ctx)]
        );
        return ctx;
    }
}