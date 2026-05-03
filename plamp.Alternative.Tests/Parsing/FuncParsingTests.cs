using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.ComplexTypes;
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
            "fn a() any { return 1; }",
            new FuncNode(
                new TypeNode(new TypeNameNode("any")), 
                new FuncNameNode("a"), [], [],
                new BodyNode([
                    new ReturnNode(new LiteralNode(1, Builtins.Int))
                ]))
        ];
        yield return
        [
            "fn b(x, y: int) { return; }",
            new FuncNode(
                new TypeNode(new TypeNameNode("")),
                new FuncNameNode("b"),
                [],
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
            fn call_any(a: any) any {
                return a;
            }
            """,
            new FuncNode(
                new TypeNode(new TypeNameNode("any")), 
                new FuncNameNode("call_any"),
                [],
                [
                    new ParameterNode(new TypeNode(new TypeNameNode("any")), new ParameterNameNode("a"))
                ],
                new BodyNode([
                    new ReturnNode(new MemberNode("a"))
                ]))
        ];
        yield return
        [
            "fn min(){}",
            new FuncNode(
                new TypeNode(new TypeNameNode("")),
                new FuncNameNode("min"),
                [],
                [],
                new BodyNode([]))
        ];
        yield return
        [
            """
            fn create_array() []int { return [1]int; }
            """,
            new FuncNode(
                new TypeNode(new TypeNameNode("int")) {ArrayDefinitions = [new()] },
                new FuncNameNode("create_array"),
                [],
                [], 
                new BodyNode([new ReturnNode(new InitArrayNode(new TypeNode(new TypeNameNode("int")), new LiteralNode(1, Builtins.Int)))]))
        ];
        yield return
        [
            """
            fn add[T] (ls: List[T], val: T) List[T] { return ls; }
            """,
            new FuncNode(
                new TypeNode(new TypeNameNode("List"), [new(new TypeNameNode("T"))]){ArrayDefinitions = []},
                new FuncNameNode("add"),
                [new GenericDefinitionNode(new GenericParameterNameNode("T"))],
                [
                    new ParameterNode(new TypeNode(new TypeNameNode("List"), [new(new TypeNameNode("T"))]){ArrayDefinitions = []}, new ParameterNameNode("ls")),
                    new ParameterNode(new TypeNode(new TypeNameNode("T")){ArrayDefinitions = []}, new ParameterNameNode("val")),
                ],
                new BodyNode([
                    new ReturnNode(new MemberNode("ls"))
                ])
            )
        ];
        yield return
        [
            """
            fn get[TKey, TVal](map: Map[TKey, TVal], key: TKey) { return; }
            """,
            new FuncNode(
                new TypeNode(new TypeNameNode(Builtins.Void.Name)),
                new FuncNameNode("get"),
                [
                    new GenericDefinitionNode(new GenericParameterNameNode("TKey")),
                    new GenericDefinitionNode(new GenericParameterNameNode("TVal"))
                ],
                [
                    new ParameterNode(new TypeNode(new TypeNameNode("Map"), [new TypeNode(new TypeNameNode("TKey")), new TypeNode(new TypeNameNode("TVal"))]), new ParameterNameNode("map")),
                    new ParameterNode(new TypeNode(new TypeNameNode("TKey")), new ParameterNameNode("key"))
                ],
                new BodyNode(
                [
                    new ReturnNode(null)
                ])
            )
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
        yield return ["fn a()", new List<string> { PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code }, false, null];
        yield return ["fn a(a: int,,b: int)", new List<string>{PlampExceptionInfo.ExpectedArgDefinition().Code}, false, null];
        yield return [
            "fn a[()", new List<string>
            {
                PlampExceptionInfo.ExpectedGenericTypeArgumentAlias().Code, 
                PlampExceptionInfo.ExpectedBodyInCurlyBrackets().Code
            }, false, null
        ];
        yield return [
            "fn a[](){}", new List<string>{PlampExceptionInfo.EmptyGenericDefinition().Code}, true, 
            new FuncNode(new TypeNode(new TypeNameNode(Builtins.Void.Name)), new FuncNameNode("a"), [], [], new BodyNode([]))
        ];
        yield return [
            "fn a[T,](){}", new List<string>{PlampExceptionInfo.ExpectedGenericTypeArgumentAlias().Code}, true, 
            new FuncNode(new TypeNode(new TypeNameNode(Builtins.Void.Name)), new FuncNameNode("a"), [new GenericDefinitionNode(new GenericParameterNameNode("T"))], [], new BodyNode([]))
        ];
        yield return [
            "fn a[T,,T](){}", new List<string>{PlampExceptionInfo.ExpectedGenericTypeArgumentAlias().Code}, true, 
            new FuncNode(
                new TypeNode(new TypeNameNode(Builtins.Void.Name)), 
                new FuncNameNode("a"), 
                [new GenericDefinitionNode(new GenericParameterNameNode("T")), new GenericDefinitionNode(new GenericParameterNameNode("T"))], 
                [], 
                new BodyNode([])
            )
        ];
        yield return ["fn a]() {}", new List<string> { PlampExceptionInfo.ExpectedOpenParen().Code }, false, null];
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