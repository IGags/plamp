using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ModuleDefinitionParsingTests
{
    public static IEnumerable<object[]> ParseModuleDefinition_Correct_DataProvider()
    {
        yield return ["module a;", new ModuleDefinitionNode("a")];
        yield return ["module a", new ModuleDefinitionNode("a")];
        yield return ["module \na", new ModuleDefinitionNode("a")];
    }
    
    [Theory]
    [MemberData(nameof(ParseModuleDefinition_Correct_DataProvider))]
    public void ParseModuleDefinition_Correct(string code, NodeBase ast)
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var result = Parser.TryParseModuleDef(context, out var module);
        result.ShouldBe(true);
        context.Exceptions.ShouldBeEmpty();
        module.ShouldBeEquivalentTo(ast);        
    }

    public static IEnumerable<object?[]> ParseModuleDefinition_Incorrect_DataProvider()
    {
        yield return ["", null, false, new List<string>()];
        yield return ["+", null, false, new List<string>()];
        yield return ["module", null, false, new List<string>{PlampExceptionInfo.ExpectedModuleName().Code}];
        yield return ["module aa.", new ModuleDefinitionNode("aa"), true, new List<string>{PlampExceptionInfo.ExpectedSubmoduleName().Code}];
        yield return ["module aa.;", new ModuleDefinitionNode("aa"), true, new List<string>{PlampExceptionInfo.ExpectedSubmoduleName().Code}];
    }
    
    [Theory]
    [MemberData(nameof(ParseModuleDefinition_Incorrect_DataProvider))]
    public void ParseModuleDefinition_Incorrect(string code, NodeBase? ast, bool resultShould, List<string> errorCodes)
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var result = Parser.TryParseModuleDef(context, out var module);
        result.ShouldBe(resultShould);
        module.ShouldBeEquivalentTo(ast);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }

    [Fact]
    public void ParseModuleDefWithTrailingTrash_RecoveryWithError()
    {
        const string code = """
                            module name hi, i am trash

                            fn a() {
                            }
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var res = Parser.ParseFile(context);
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.ExpectedEndOfStatement().Code);
        res.ModuleName.ShouldNotBeNull().ModuleName.ShouldBe("name");
    }
}