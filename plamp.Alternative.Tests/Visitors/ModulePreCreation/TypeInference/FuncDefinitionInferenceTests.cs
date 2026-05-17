using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Alternative.Visitors.SymbolTableBuilding.FuncDefInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class FuncDefinitionInferenceTests
{
    [Fact]
    public void FunctionWithDuplicateParameterNames_DoesNotThrow()
    {
        const string code = """
                            fn a(x, x: int) {}
                            """;
        var (ast, parsingCtx) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var visitor = new TypeInferenceWeaver();
        var preCreationContext = new PreCreationContext(parsingCtx.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        
        var exception = Record.Exception(() => visitor.WeaveDiffs(ast, preCreationContext));
        
        exception.ShouldBeNull();
    }

    [Fact]
    public void HandleFuncWithTwoArgs_Correct()
    {
        const string code = """
                            fn nop(a :int, b :string) {}
                            """;
        
        var (context, ast) = Setup(code);
        var visitor = new TypeInferenceWeaver();
        var weaveResult = Should.NotThrow(() => visitor.WeaveDiffs(ast, context));
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void VariablesNotSharedBetweenFunctions_Correct()
    {
        //Если переменные будут протекать из функции в функцию, то в теле одной из функций будет memberNode, а не variableDefinition
        const string code = """
                            module a;
                            fn f1() int {
                                a := 1;
                                return a;
                            }
                            
                            fn f2() string {
                                a := "1";
                                return a;
                            }
                            """;
        var (context, ast) = Setup(code);
        var visitor = new TypeInferenceWeaver();
        visitor.WeaveDiffs(ast, context);
        
        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var assignExpressions = root.Functions.Select(x => x.Body.ExpressionList[0]).Cast<AssignNode>();
        foreach (var assignNode in assignExpressions)
        {
            assignNode.Targets.ShouldHaveSingleItem().ShouldBeOfType<VariableDefinitionNode>();
        }
    }

    [Fact]
    public void ArgsNotSharedBetweenFunctions_Correct()
    {
        const string code = """
                            fn f1(a: int) int { return a; }
                            fn f2(a: string) string { return a; }
                            """;
        
        var (context, ast) = Setup(code);
        var visitor = new TypeInferenceWeaver();
        visitor.WeaveDiffs(ast, context);
        
        context.Exceptions.ShouldBeEmpty();
        var root = ast.ShouldBeOfType<RootNode>();
        var f1 = root.Functions.Single(x => x.FuncName.Value == "f1");
        var f2 = root.Functions.Single(x => x.FuncName.Value == "f2");
        
        f1.ParameterList.ShouldHaveSingleItem().Type.TypeName.Name.ShouldBe("int");
        f2.ParameterList.ShouldHaveSingleItem().Type.TypeName.Name.ShouldBe("string");
    }

    [Fact]
    public void AssignToArgIncorrectType_ReturnsIncorrect()
    {
        const string code = """
                            fn faceless(name: string) {
                                name := 1;
                            }
                            """;
        var (context, ast) = Setup(code);
        var visitor = new TypeInferenceWeaver();
        visitor.WeaveDiffs(ast, context);
        var error = context.Exceptions.ShouldHaveSingleItem();
        error.Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
    }

    [Fact]
    public void UseArgWithUnknownType_ReturnsExceptionOnArgOnly()
    {
        const string code = """
                            fn faced(name: face) int {
                                return name.age;
                            }
                            """;
        var (context, ast) = Setup(code);
        var visitor = new TypeInferenceWeaver();
        visitor.WeaveDiffs(ast, context);
        var error = context.Exceptions.ShouldHaveSingleItem();
        error.Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound(string.Empty).Code);
    }

    private (PreCreationContext, NodeBase) Setup(string code)
    {
        var funcDefWeaver = new FuncDefInferenceWeaver();
        var (context, ast) = CompilationPipelineBuilder.RunSymTableVisitors(code,
        [
            (ast, cxt) => funcDefWeaver.WeaveDiffs(ast, cxt)
        ]);
        
        var preCreationContext = new PreCreationContext(context.TranslationTable, context.Dependencies.ToList());
        return (preCreationContext, ast);
    }
}