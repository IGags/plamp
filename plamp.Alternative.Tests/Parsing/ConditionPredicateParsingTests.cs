using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class ConditionPredicateParsingTests
{
    public static IEnumerable<object[]> ParseConditionPredicate_Correct_DataProvider()
    {
        yield return ["(a)", new MemberNode("a")];
        yield return ["(true)", new LiteralNode(true, typeof(bool))];
        yield return ["(fn1())", new CallNode(null, new FuncCallNameNode("fn1"), [])];
    }
    
    [Theory]
    [MemberData(nameof(ParseConditionPredicate_Correct_DataProvider))]
    public void ParseConditionPredicate_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseConditionPredicate(context, out var predicate);
        context.Exceptions.ShouldBeEmpty();
        result.ShouldBe(true);
        predicate.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object?[]> ParseConditionPredicate_Incorrect_DataProvider()
    {
        yield return ["true)", new List<string> { PlampExceptionInfo.ExpectedOpenParen().Code }, null, false];
        yield return ["(a &&", new List<string> {PlampExceptionInfo.ExpectedCloseParen().Code }, new MemberNode("a"), true];
        yield return ["(a &&", new List<string> {PlampExceptionInfo.ExpectedCloseParen().Code }, new MemberNode("a"), true];
        yield return ["(++", new List<string> {PlampExceptionInfo.ExpectedExpression().Code }, null, false];
        yield return ["++", new List<string> {PlampExceptionInfo.ExpectedOpenParen().Code }, null, false];
    }
    
    [Theory]
    [MemberData(nameof(ParseConditionPredicate_Incorrect_DataProvider))]
    public void ParseConditionPredicate_Incorrect(string code, List<string> errorCodes, NodeBase? ast, bool result)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseConditionPredicate(context, out var conditionPredicate);
        parsed.ShouldBe(result);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
        conditionPredicate.ShouldBeEquivalentTo(ast);
    }
}