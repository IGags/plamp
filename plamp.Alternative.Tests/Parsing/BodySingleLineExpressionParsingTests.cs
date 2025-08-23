using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class BodySingleLineExpressionParsingTests
{
    public static IEnumerable<object[]> ParseBodySingleLineExpression_Correct_DataProvider()
    {
        yield return ["a++", new PostfixIncrementNode(new MemberNode("a"))];
        yield return ["a()", new CallNode(null, new MemberNode("a"), [])];
        yield return ["a := 41", new AssignNode(new MemberNode("a"), new LiteralNode(41, typeof(int)))];
        yield return ["!a", new NotNode(new MemberNode("a"))];
        yield return ["int a", new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))];
    }
    
    [Theory]
    [MemberData(nameof(ParseBodySingleLineExpression_Correct_DataProvider))]
    public void ParseBodySingleLineExpression_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseExpression(context, out var expression);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        expression.ShouldBeEquivalentTo(ast);
    }

    [Fact]
    public static void ParseBodySingleLineExpression_Incorrect()
    {
        const string code = "+-fn";
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseExpression(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.Select(x => x.Code)
            .ShouldHaveSingleItem().ShouldBe(PlampExceptionInfo.ExpectedExpression().Code);
    }
}