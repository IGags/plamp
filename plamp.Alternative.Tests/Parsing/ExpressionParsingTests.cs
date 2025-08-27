using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Binary;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ExpressionParsingTests
{
    public static IEnumerable<object[]> ParseSimpleNud_DataProvider()
    {
        yield return ["--1", new PrefixDecrementNode(new LiteralNode(1, typeof(int)))];
        yield return ["++1", new PrefixIncrementNode(new LiteralNode(1, typeof(int)))];
        yield return ["+1", new LiteralNode(1, typeof(int))];
        yield return ["-1", new UnaryMinusNode(new LiteralNode(1, typeof(int)))];
        yield return ["!true", new NotNode(new LiteralNode(true, typeof(bool)))];
        yield return ["(true)", new LiteralNode(true, typeof(bool))];
        yield return ["(((true)))", new LiteralNode(true, typeof(bool))];
        yield return ["greet_you()", new CallNode(null, new FuncCallNameNode("greet_you"), [])];
        yield return ["greet_you(1, 2, a)", new CallNode(null, new FuncCallNameNode("greet_you"), [new LiteralNode(1, typeof(int)), new LiteralNode(2, typeof(int)), new MemberNode("a")])];
        yield return ["a", new MemberNode("a")];
        yield return ["\"a\"", new LiteralNode("a", typeof(string))];
    }
    
    [Theory]
    [MemberData(nameof(ParseSimpleNud_DataProvider))]
    public void ParseSimpleNud_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseNud(context, out var node);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseIncorrectNud_DataProvider()
    {
        yield return
        [
            "(true", new List<PlampException>
            {
                new(
                    PlampExceptionInfo.ExpectedCloseParen(),
                    new FilePosition(0, 5), new FilePosition(0, 5),
                    "any.plp")
            },
            true
        ];
        yield return ["+", new List<PlampException>(), false];
    }
    
    [Theory]
    [MemberData(nameof(ParseIncorrectNud_DataProvider))]
    public void ParseSimpleNud_Incorrect(string code, List<PlampException> exception, bool expectedResult)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseNud(context, out _);
        parsed.ShouldBe(expectedResult);
        var exceptionsShould = ExcludeFields(exception);
        var exceptionsActual = ExcludeFields(context.Exceptions);
        exceptionsActual.ShouldBeEquivalentTo(exceptionsShould);
        object ExcludeFields(List<PlampException> exceptions)
        {
            return exceptions.Select(x => new { x.Code, x.EndPosition, x.Level, x.StartPosition }).ToList();
        }
    }

    public static IEnumerable<object[]> ParseExpressionWithPostfix_Correct_DataProvider()
    {
        yield return ["a++", new PostfixIncrementNode(new MemberNode("a"))];
        yield return ["a--", new PostfixDecrementNode(new MemberNode("a"))];
    }
    
    [Theory]
    [MemberData(nameof(ParseExpressionWithPostfix_Correct_DataProvider))]
    public void ParseExpressionWithPostfix_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParsePrecedence(context, out var node);
        parsed.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseBinaryExpression_Correct_DataProvider()
    {
        yield return ["a + 1", new AddNode(new MemberNode("a"), new LiteralNode(1, typeof(int)))];
        yield return ["a - 1", new SubNode(new MemberNode("a"), new LiteralNode(1, typeof(int)))];
        yield return ["a * 1", new MulNode(new MemberNode("a"), new LiteralNode(1, typeof(int)))];
        yield return ["a / 1", new DivNode(new MemberNode("a"), new LiteralNode(1, typeof(int)))];
        yield return ["true = a", new EqualNode(new LiteralNode(true, typeof(bool)), new MemberNode("a"))];
        yield return ["a != 5", new NotEqualNode(new MemberNode("a"), new LiteralNode(5, typeof(int)))];
        yield return ["1 < 2", new LessNode(new LiteralNode(1, typeof(int)), new LiteralNode(2, typeof(int)))];
        yield return ["1 > 2", new GreaterNode(new LiteralNode(1, typeof(int)), new LiteralNode(2, typeof(int)))];
        yield return ["1 <= 2", new LessOrEqualNode(new LiteralNode(1, typeof(int)), new LiteralNode(2, typeof(int)))];
        yield return ["1 >= 2", new GreaterOrEqualNode(new LiteralNode(1, typeof(int)), new LiteralNode(2, typeof(int)))];
        yield return ["x || y", new OrNode(new MemberNode("x"), new MemberNode("y"))];
        yield return ["a && b", new AndNode(new MemberNode("a"), new MemberNode("b"))];
        yield return ["x := 3", new AssignNode(new MemberNode("x"), new LiteralNode(3, typeof(int)))];
    }
    
    [Theory]
    [MemberData(nameof(ParseBinaryExpression_Correct_DataProvider))]
    public void ParseBinaryExpression_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParsePrecedence(context, out var node);
        parsed.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }
    
    [Fact]
    public void ParseBinaryExpression_Incorrect()
    {
        var code = "a + ";
        var ast = new MemberNode("a");
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParsePrecedence(context, out var node);
        parsed.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> CheckPrecedence_Correct_DataProvider()
    {
        yield return
        [
            "a + b * c", new AddNode(new MemberNode("a"), new MulNode(new MemberNode("b"), new MemberNode("c")))
        ];
        yield return
        [
            "(a + b) * c", new MulNode(new AddNode(new MemberNode("a"), new MemberNode("b")), new MemberNode("c"))
        ];
    }
    
    [Theory]
    [MemberData(nameof(CheckPrecedence_Correct_DataProvider))]
    public void CheckPrecedence_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParsePrecedence(context, out var node);
        parsed.ShouldBe(true);
        node.ShouldBeEquivalentTo(ast);
    }
}