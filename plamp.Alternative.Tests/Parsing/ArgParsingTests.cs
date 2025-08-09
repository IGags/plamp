using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ArgParsingTests
{
    [Fact]
    public void ParseArg_Correct()
    {
        const string code = "int abc";
        var ast = new ParameterNode(new TypeNode(new MemberNode("int")), new MemberNode("abc"));
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArg(context, out var arg);
        result.ShouldBe(true);
        arg.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseArg_Incorrect_DataProvider()
    {
        yield return ["", new List<string>()];
        yield return ["+", new List<string>()];
        yield return ["int", new List<string>{ PlampExceptionInfo.ExpectedArgName().Code }];
        yield return ["int +", new List<string>{ PlampExceptionInfo.ExpectedArgName().Code }];
        yield return ["int fn", new List<string>{ PlampExceptionInfo.ExpectedArgName().Code }];
    }
    
    [Theory]
    [MemberData(nameof(ParseArg_Incorrect_DataProvider))]
    public void ParseArg_Incorrect(string code, List<string> errorCodes)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseArg(context, out var arg);
        result.ShouldBe(false);
        arg.ShouldBeNull();
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }
}