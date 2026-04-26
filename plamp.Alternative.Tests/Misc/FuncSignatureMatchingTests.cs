using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.SymbolsBuildingImpl;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Misc;

public class FuncSignatureMatchingTests

{
    [Fact]
    //Модулей нет
    public void EmptyModules_ReturnsNotFoundError()
    {
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("a", [], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.FunctionIsNotFound("").Code);
    }
    
    [Fact]
    //Функции нет в модулях
    public void FuncNotFoundInModules_ReturnsNotFoundError()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [],
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("a", [builder], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.FunctionIsNotFound("").Code);
    }

    [Fact]
    //Функции совпадают полностью
    public void FuncMatchCompletely_Correct()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        var fnInfo = builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [], [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [builder], out var info);
        record.ShouldBeNull();
        info.ShouldBe(fnInfo);
    }

    [Fact]
    //Разное число аргументов
    public void FuncArgCountDoesNotMatch_OtherwiseReturnsFunction()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [], [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [builder], out var info);
        info.ShouldNotBeNull();
        record.ShouldBeNull();
    }

    [Fact]
    //Две функции подходят.
    public void TwoFunctionsMatches_ReturnsException()
    {
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        
        var firstBuilder = new SymTableBuilder();
        firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [], [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var secondBuilder = new SymTableBuilder();
        secondBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [], [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [firstBuilder, secondBuilder], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.AmbiguousFunctionReference("", []).Code);
    }
}