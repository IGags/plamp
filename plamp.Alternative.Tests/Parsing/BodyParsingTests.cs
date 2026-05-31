using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ControlFlow;
using plamp.Abstractions.Ast.Node.Unary;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class BodyParsingTests
{
    public static IEnumerable<object[]> ParseBody_Correct_DataProvider()
    {
        yield return ["{}", new BodyNode([])];
        yield return [";", new BodyNode([])];
        yield return ["return;", new BodyNode([new ReturnNode(null)])];
        yield return ["{a; b;}", new BodyNode([new MemberNode("a"), new MemberNode("b")])];
        yield return
        [
            """
            {
                a := b;
                b := a;
            }
            """,
            new BodyNode([
                new AssignNode([new MemberNode("a")], [new MemberNode("b")]),
                new AssignNode([new MemberNode("b")], [new MemberNode("a")])
            ])
        ];
        //Парсер не проверяет корректность нахождения выражения внутри body
        //После инкремента выражение должно завершаться в отличие от бинарных операторов
        yield return
        [
            """
            {
                a++
                b
            }
            """,
            new BodyNode([
                new PostfixIncrementNode(new MemberNode("a")),
                new MemberNode("b")
            ])
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseBody_Correct_DataProvider))]
    public void ParseBody_Correct(string code, NodeBase ast)
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var result = Parser.TryParseBody(context, out var body);
        context.Exceptions.ShouldBeEmpty();
        result.ShouldBe(true);
        body.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseBody_Incorrect_DataProvider()
    {
        yield return
        [
            "{ aaa; ", new BodyNode([new MemberNode("aaa")]),
            new List<string> { PlampExceptionInfo.ExpectedClosingCurlyBracket().Code }
        ];
    }
    
    //Body parsing always true.
    //Check only body parsing generated errors, not underlying methods.
    [Theory]
    [MemberData(nameof(ParseBody_Incorrect_DataProvider))]
    public void ParseBody_Incorrect(string code, NodeBase ast, List<string> errorCodes)
    {
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var result = Parser.TryParseBody(context, out var body);
        result.ShouldBe(true);
        body.ShouldBeEquivalentTo(ast);
        foreach (var errorCode in errorCodes)
        {
            context.Exceptions.Select(x => x.Code).ShouldContain(errorCode);
        }
    }

    [Fact]
    public void TrashAfterCloseParen_Correct()
    {
        const string code = """
                            {
                                if (true) {
                                    a := b
                                } return a
                            }
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var correct = Parser.TryParseBody(context, out var body);
        correct.ShouldBeTrue();
        context.Exceptions.ShouldBeEmpty();
        body.ShouldNotBeNull().ExpressionList.Count.ShouldBe(2);
    }


    [Fact]
    public void BodyInBody_Incorrect()
    {
        const string code = """
                            {
                                {
                                    a: int
                                } 
                            }
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var correct = Parser.TryParseBody(context, out var body);
        correct.ShouldBeTrue();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.UnexpectedToken("{").Code);
        body.ShouldNotBeNull().ExpressionList.ShouldBeEmpty();
    }
    
    /// <summary>
    /// Восстановление потока токенов происходит на следующем end of line это означает, что первое выражение будет утеряно при синхронизации потока токенов.
    /// </summary>
    [Fact]
    public void BodyInBody_IncorrectRecovery()
    {
        const string code = """
                            {
                                {
                                    a: int
                                    b: int
                                } 
                            }
                            """;
        var context = CompilationPipelineBuilder.CreateParsingContext(code);
        var correct = Parser.TryParseBody(context, out var body);
        correct.ShouldBeTrue();
        var ex = context.Exceptions.ShouldHaveSingleItem();
        ex.Code.ShouldBe(PlampExceptionInfo.UnexpectedToken("{").Code);
        body.ShouldNotBeNull().ExpressionList.ShouldHaveSingleItem();
    }
}