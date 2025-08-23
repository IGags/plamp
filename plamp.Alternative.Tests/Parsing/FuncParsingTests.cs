using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class FuncParsingTests
{
    public static IEnumerable<object[]> ParseFunc_Correct_DataProvider()
    {
        yield return
        [
            "fn a() any return 1;",
            new FuncNode(
                new TypeNode(new TypeNameNode("any")), 
                new FuncNameNode("a"), [],
                new BodyNode([
                    new ReturnNode(new LiteralNode(1, typeof(int)))
                ]))
        ];
        yield return
        [
            "fn b(int x, int y) return;",
            new FuncNode(
                null,
                new FuncNameNode("b"),
                [
                    new ParameterNode(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("x")),
                    new ParameterNode(new TypeNode(new TypeNameNode("int")), new ParameterNameNode("y"))
                ],
                new BodyNode([
                    new ReturnNode(null)
                ]))
        ];
        yield return
        [
            """
            fn call_any(any a) any {
                return a;
            }
            """,
            new FuncNode(
                new TypeNode(new TypeNameNode("any")), 
                new FuncNameNode("call_any"), 
                [
                    new ParameterNode(new TypeNode(new TypeNameNode("any")), new ParameterNameNode("a"))
                ],
                new BodyNode([
                    new ReturnNode(new MemberNode("a"))
                ]))
        ];
        yield return
        [
            "fn min();",
            new FuncNode(
                null,
                new FuncNameNode("min"),
                [],
                new BodyNode([]))
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseFunc_Correct_DataProvider))]
    public void ParseFunc_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseFunc(context, out var func);
        result.ShouldBe(true);
        context.Exceptions.ShouldBeEmpty();
        func.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object?[]> ParseFunc_Incorrect_DataProvider()
    {
        yield return ["", new List<string>(), false, null];
        yield return ["+", new List<string>(), false, null];
        yield return ["fn", new List<string>{PlampExceptionInfo.ExpectedFuncName().Code}, false, null];
        yield return ["fn a(", new List<string>{PlampExceptionInfo.ExpectedArgDefinition().Code}, false, null];
        yield return
        [
            "fn a()", new List<string> { PlampExceptionInfo.UnexpectedToken("").Code }, true,
            new FuncNode(null, new FuncNameNode("a"), [], new BodyNode([]))
        ];
        yield return ["fn a(int a,,int b)", new List<string>{PlampExceptionInfo.ExpectedArgDefinition().Code}, false, null];
    }

    [Theory]
    [MemberData(nameof(ParseFunc_Incorrect_DataProvider))]
    public void ParseFunc_Incorrect(string code, List<string> errorCodes, bool resultShould, NodeBase? astShould)
    {
        var fixture = new Fixture { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseFunc(context, out var func);
        result.ShouldBe(resultShould);
        func.ShouldBeEquivalentTo(astShould);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }
}