using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class FunctionCallParsingTests
{
    public static IEnumerable<object[]> ParseFuncCall_Correct_DataProvider()
    {
        yield return ["fn1()", new CallNode(null, new MemberNode("fn1"), [])];
        yield return ["fn2(a)", new CallNode(null, new MemberNode("fn2"), [new MemberNode("a")])];
        yield return
        [
            "fn3(a, b, c)",
            new CallNode(null, new MemberNode("fn3"), [new MemberNode("a"), new MemberNode("b"), new MemberNode("c")])
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseFuncCall_Correct_DataProvider))]
    public void ParseFuncCall_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseFuncCall(context, out var call);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        call.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object?[]> ParseFuncCall_Incorrect_DataProvider()
    {
        yield return ["123", new List<string>(), false, null];
        yield return 
        [
            "fn1", new List<string> { PlampExceptionInfo.ExpectedOpenParen().Code }, false,
            null
        ];
        yield return 
        [
            "fn2(", new List<string>() { PlampExceptionInfo.ExpectedExpression().Code }, false,
            null
        ];
        yield return 
        [
            "fn3(a", new List<string>() { PlampExceptionInfo.ExpectedCloseParen().Code }, true,
            new CallNode(null, new MemberNode("fn3"), [new MemberNode("a")])
        ];
        yield return 
        [
            "fn4(a, b", new List<string>() { PlampExceptionInfo.ExpectedCloseParen().Code }, true,
            new CallNode(null, new MemberNode("fn4"), [new MemberNode("a"), new MemberNode("b")])
        ];
        yield return 
        [
            "fn5(a b", new List<string>() { PlampExceptionInfo.ExpectedCloseParen().Code }, true,
            new CallNode(null, new MemberNode("fn5"), [new MemberNode("a")])
        ];
        yield return 
        [
            "fn6(a, +", new List<string>() { PlampExceptionInfo.ExpectedExpression().Code }, false,
            null
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseFuncCall_Incorrect_DataProvider))]
    public void ParseFuncCall_Incorrect(string code, List<string> errorCodes, bool result, NodeBase? ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseFuncCall(context, out var call);
        parsed.ShouldBe(result);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
        call.ShouldBeEquivalentTo(ast);
    }
}