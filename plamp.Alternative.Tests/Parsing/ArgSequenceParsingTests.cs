using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ArgSequenceParsingTests
{
    public static IEnumerable<object[]> ParseArgSequence_Correct_DataProvider()
    {
        yield return ["()", new List<ParameterNode>()];
        yield return ["(int a)", new List<ParameterNode> { new(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("a")) }];
        yield return
        [
            "(int a, str b, double c)",
            new List<ParameterNode>
            {
                new(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("a")),
                new(new TypeNode(new TypeNameNode("str")), new ParameterNameNode("b")),
                new(new TypeNode(new TypeNameNode("double")), new ParameterNameNode("c")),
            }
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseArgSequence_Correct_DataProvider))]
    public void ParseArgSequence_Correct(string code, List<ParameterNode> astShould)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArgSequence(context, out var list);
        result.ShouldBe(true);
        context.Exceptions.ShouldBeEmpty();
        list.ShouldBeEquivalentTo(astShould);
    }

    public static IEnumerable<object?[]> ParseArgSequence_Incorrect_DataProvider()
    {
        yield return ["", null, false, new List<string>{PlampExceptionInfo.ExpectedOpenParen().Code}];
        yield return ["abc", null, false, new List<string>{PlampExceptionInfo.ExpectedOpenParen().Code}];
        yield return ["(,", null, false, new List<string>{PlampExceptionInfo.ExpectedArgDefinition().Code}];
        yield return 
        [
            "(int a,, int b", 
            new List<ParameterNode>{new(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("a"))}, 
            false, 
            new List<string>{PlampExceptionInfo.ExpectedArgDefinition().Code}
        ];
        yield return
        [
            "(int a",
            new List<ParameterNode>{new(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("a"))},
            true,
            new List<string>{PlampExceptionInfo.ExpectedCloseParen().Code}
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseArgSequence_Incorrect_DataProvider))]
    public void ParseArgSequence_Incorrect(string code, List<ParameterNode>? listShould, bool resultShould, List<string> errorCodes)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArgSequence(context, out var list);
        result.ShouldBe(resultShould);
        list.ShouldBeEquivalentTo(listShould);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }
}