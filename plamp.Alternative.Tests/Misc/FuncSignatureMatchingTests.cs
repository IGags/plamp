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
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("a", [], [], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.FunctionIsNotFound("", []).Code);
    }
    
    [Fact]
    //Функции нет в модулях
    public void FuncNotFoundInModules_ReturnsNotFoundError()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("a", [], [builder], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.FunctionIsNotFound("", []).Code);
    }

    [Fact]
    //Функции совпадают полностью
    public void FuncMatchCompletely_Correct()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        var fnInfo = builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [Builtins.Int], [builder], out var info);
        record.ShouldBeNull();
        info.ShouldBe(fnInfo);
    }

    [Fact]
    //Разное число аргументов
    public void FuncArgCountDoesNotMatch_ReturnsNotFoundError()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [], [builder], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.FunctionIsNotFound("", []).Code);
    }
    
    [Fact]
    //Функция подходит с точностью до неявного каста
    public void FuncMatchWithCastAccuracy_Correct()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        var fnInfo = builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [Builtins.Short], [builder], out var info);
        record.ShouldBeNull();
        info.ShouldBe(fnInfo);
    }
    
    [Fact]
    //Функция без части известных типов подходит
    public void FindFuncWithoutPartOfSignature_Correct()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        var fnInfo = builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [null], [builder], out var info);
        record.ShouldBeNull();
        info.ShouldBe(fnInfo);
    }

    [Fact]
    //Две функции одна подходит
    public void TwoFunctionsOneMatch_Correct()
    {
        var builder = new SymTableBuilder();
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        var firstFn = builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        builder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"), [], new BodyNode([])));
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [Builtins.Int], [builder], out var info);
        record.ShouldBeNull();
        info.ShouldBe(firstFn);
    }

    [Fact]
    //Две функции подходят.
    public void TwoFunctionsMatches_ReturnsException()
    {
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        
        var firstBuilder = new SymTableBuilder();
        firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var secondBuilder = new SymTableBuilder();
        firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [Builtins.Int], [firstBuilder, secondBuilder], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.AmbigulousFunctionReference("", [], []).Code);
    }

    [Fact]
    //Две функции подходят с точностью до каста.
    public void TwoFunctionsMatchesWithCastAccuracy_ReturnsException()
    {
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var intType = new TypeNode(new TypeNameNode("int")) { ArrayDefinitions = [], TypeInfo = Builtins.Int };
        var longType = new TypeNode(new TypeNameNode("long")) { ArrayDefinitions = [], TypeInfo = Builtins.Long };
        
        var firstBuilder = new SymTableBuilder();
        firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(intType, new ParameterNameNode("a"))], new BodyNode([])));
        var secondBuilder = new SymTableBuilder();
        firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(longType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [Builtins.Short], [firstBuilder, secondBuilder], out var info);
        info.ShouldBeNull();
        record.ShouldNotBeNull().Code.ShouldBe(PlampExceptionInfo.AmbigulousFunctionReference("", [], []).Code);
    }
    
    [Fact]
    //Две функции одна подходит полностью, вторая с точностью до каста.
    public void TwoFunctionsMatchesOneFullyOtherPartially_Correct()
    {
        var emptyRetType = new TypeNode(new TypeNameNode("")) { ArrayDefinitions = [], TypeInfo = Builtins.Void };
        var shortType = new TypeNode(new TypeNameNode("short")) { ArrayDefinitions = [], TypeInfo = Builtins.Short };
        var longType = new TypeNode(new TypeNameNode("long")) { ArrayDefinitions = [], TypeInfo = Builtins.Long };
        
        var firstBuilder = new SymTableBuilder();
        var firstFn = firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(shortType, new ParameterNameNode("a"))], new BodyNode([])));
        var secondBuilder = new SymTableBuilder();
        firstBuilder.DefineFunc(new FuncNode(emptyRetType, new FuncNameNode("abc"),
            [new ParameterNode(longType, new ParameterNameNode("a"))], new BodyNode([])));
        
        var record = SymbolSearchUtility.TryGetFuncOrErrorRecord("abc", [Builtins.Short], [firstBuilder, secondBuilder], out var info);
        record.ShouldBeNull();
        info.ShouldBe(firstFn);
    }
}