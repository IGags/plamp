using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ReturnParsingTests
{
    public static IEnumerable<object[]> ParseReturn_Correct_DataProvider()
    {
        yield return ["return;", new ReturnNode(null)];
        yield return ["return fn1();", new ReturnNode(new CallNode(null, new FuncCallNameNode("fn1"), []))];
        yield return ["return a;", new ReturnNode(new MemberNode("a"))];
        yield return ["return a++;", new ReturnNode(new PostfixIncrementNode(new MemberNode("a")))];
        yield return ["return a + b;", new ReturnNode(new AddNode(new MemberNode("a"), new MemberNode("b")))];
        yield return ["return !a;", new ReturnNode(new NotNode(new MemberNode("a")))];
    } 
    
    [Theory]
    [MemberData(nameof(ParseReturn_Correct_DataProvider))]
    public void ParseReturn_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseReturn(context, out var returnNode);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        returnNode.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object?[]> ParseReturn_Incorrect_DataProvider()
    {
        yield return ["1", new List<string>(), null, false];
        yield return ["return", new List<string>{PlampExceptionInfo.ExpectedExpression().Code}, null, false];
        yield return ["return +", new List<string> { PlampExceptionInfo.ExpectedExpression().Code }, null, false];
        yield return
        [
            "return 1", new List<string> { PlampExceptionInfo.ExpectedEndOfStatement().Code },
            new ReturnNode(new LiteralNode(1, typeof(int))), true
        ];
    } 
    
    [Theory]
    [MemberData(nameof(ParseReturn_Incorrect_DataProvider))]
    public void ParseReturn_Incorrect(string code, List<string> errorCodes, NodeBase? ast, bool result)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseReturn(context, out var returnNode);
        parsed.ShouldBe(result);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
        returnNode.ShouldBeEquivalentTo(ast);
    }
}