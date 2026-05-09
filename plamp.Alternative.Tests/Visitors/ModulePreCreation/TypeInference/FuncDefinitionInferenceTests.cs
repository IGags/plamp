using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class FuncDefinitionInferenceTests
{
    [Fact]
    public async Task EntirePipelineWithDuplicateParameterNames_ReturnsValidationErrors()
    {
        const string code = """
                            module test;
                            fn a(x, x: int) {}
                            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(code));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        
        CompilationPipeline.CompilationRes result = default;
        var exception = await Record.ExceptionAsync(async () =>
            result = await CompilationPipeline.RunEntirePipelineAsync(reader, "test.plamp"));
        
        exception.ShouldBeNull();
        result.Compiled.ShouldBeNull();
        result.Exceptions.Select(x => x.Code)
            .ShouldAllBe(x => x == PlampExceptionInfo.DuplicateParameterName().Code);
    }

    [Fact]
    public void HandleFuncWithTwoArgs_Correct()
    {
        const string code = """
                            fn nop(a :int, b :string) {}
                            """;
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var weaveResult = Should.NotThrow(() => visitor.WeaveDiffs(expression!, preCreation));
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
        var (ast, parsingCtx) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var visitor = new TypeInferenceWeaver();
        var preCreationContext = new PreCreationContext(parsingCtx.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        visitor.WeaveDiffs(ast, preCreationContext);
        
        preCreationContext.Exceptions.ShouldBeEmpty();
        var assignExpressions = ast.Functions.Select(x => x.Body.ExpressionList[0]).Cast<AssignNode>();
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
        
        var (ast, parsingCtx) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var visitor = new TypeInferenceWeaver();
        var preCreationContext = new PreCreationContext(parsingCtx.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        visitor.WeaveDiffs(ast, preCreationContext);
        
        preCreationContext.Exceptions.ShouldBeEmpty();
        var f1 = ast.Functions.Single(x => x.FuncName.Value == "f1");
        var f2 = ast.Functions.Single(x => x.FuncName.Value == "f2");
        
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
        var (ast, parsingCtx) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var visitor = new TypeInferenceWeaver();
        var preCreationContext = new PreCreationContext(parsingCtx.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        visitor.WeaveDiffs(ast, preCreationContext);
        var error = preCreationContext.Exceptions.ShouldHaveSingleItem();
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
        var (ast, parsingCtx) = CompilationPipelineBuilder.RunParsingPipeline(code);
        var visitor = new TypeInferenceWeaver();
        var preCreationContext = new PreCreationContext(parsingCtx.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        visitor.WeaveDiffs(ast, preCreationContext);
        var error = preCreationContext.Exceptions.ShouldHaveSingleItem();
        error.Code.ShouldBe(PlampExceptionInfo.TypeIsNotFound(string.Empty).Code);
    }
}