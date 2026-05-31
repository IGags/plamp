using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class FunctionCallParsingTests
{
    public static IEnumerable<object[]> ParseFuncCall_Correct_DataProvider()
    {
        yield return ["fn1()", new CallNode(null, new FuncCallNameNode("fn1"), [], [])];
        yield return ["fn2(a)", new CallNode(null, new FuncCallNameNode("fn2"), [new MemberNode("a")], [])];
        yield return
        [
            "fn3(a, b, c)",
            new CallNode(null, new FuncCallNameNode("fn3"), [new MemberNode("a"), new MemberNode("b"), new MemberNode("c")], [])
        ];
        yield return ["fn4[T]()", new CallNode(null, new FuncCallNameNode("fn4"), [], [new TypeNode(new TypeNameNode("T"))])];
        yield return
            [
                "fn5[T, T2]()",
                new CallNode(null, new FuncCallNameNode("fn5"), [],
                    [new TypeNode(new TypeNameNode("T")), new TypeNode(new TypeNameNode("T2"))])
            ];
        yield return
        [
            "fn6[T, T](a, b)",
            new CallNode(null, new FuncCallNameNode("fn6"), [new MemberNode("a"), new MemberNode("b")],
                [new TypeNode(new TypeNameNode("T")), new TypeNode(new TypeNameNode("T"))])
        ];
        yield return
        [
            "fn7(a, a)", new CallNode(null, new FuncCallNameNode("fn7"), [new MemberNode("a"), new MemberNode("a")], [])
        ];
        yield return
        [
            "fn8[[]int, []List[int]]()",
            new CallNode(null, new FuncCallNameNode("fn8"), [], [
                new TypeNode(new TypeNameNode("int")){ArrayDefinitions = [new ArrayTypeSpecificationNode()]},
                new TypeNode(new TypeNameNode("List"), [new TypeNode(new TypeNameNode("int"))]) {ArrayDefinitions = [new ArrayTypeSpecificationNode()]}
            ])
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
            new CallNode(null, new FuncCallNameNode("fn3"), [new MemberNode("a")], [])
        ];
        yield return 
        [
            "fn4(a, b", new List<string>() { PlampExceptionInfo.ExpectedCloseParen().Code }, true,
            new CallNode(null, new FuncCallNameNode("fn4"), [new MemberNode("a"), new MemberNode("b")], [])
        ];
        yield return 
        [
            "fn5(a b", new List<string>() { PlampExceptionInfo.ExpectedCloseParen().Code }, true,
            new CallNode(null, new FuncCallNameNode("fn5"), [new MemberNode("a")], [])
        ];
        yield return 
        [
            "fn6(a, +", new List<string>() { PlampExceptionInfo.ExpectedExpression().Code }, false,
            null
        ];
        yield return
        [
            "fn7[]()", new List<string>() { PlampExceptionInfo.EmptyGenericArgs().Code }, true,
            new CallNode(null, new FuncCallNameNode("fn7"), [], [])
        ];
        yield return
        [
            "fn8[()", new List<string>()
            {
                PlampExceptionInfo.ExpectedGenericArg().Code,
                PlampExceptionInfo.GenericArgsIsNotClosed().Code
            }, true,
            new CallNode(null, new FuncCallNameNode("fn8"), [], [])
        ];
        yield return 
        [
            "fn9[T,]()", new List<string>() {  
                PlampExceptionInfo.ExpectedGenericArg().Code
            }, true,
            new CallNode(null, new FuncCallNameNode("fn9"), [], [new TypeNode(new TypeNameNode("T"))])
        ];
        yield return
        [
            "fn10[T[int,]()", new List<string>()
            {
                PlampExceptionInfo.ExpectedGenericArg().Code,
                PlampExceptionInfo.GenericArgsIsNotClosed().Code
            }, true,
            new CallNode(null, new FuncCallNameNode("fn10"), [], [new TypeNode(new TypeNameNode("T"), [new TypeNode(new TypeNameNode("int"))])])
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