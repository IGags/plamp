using System.Collections.Generic;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class FuncParsingTests
{
    public static IEnumerable<object[]> ParseFunc_Correct_DataProvider()
    {
        
    }
    
    [Theory]
    [MemberData(nameof(ParseFunc_Correct_DataProvider))]
    public void ParseFunc_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseFunc(context, out var func);
        result.ShouldBe(true);
        func.ShouldBeEquivalentTo(ast);
    }
}