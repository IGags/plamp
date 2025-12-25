using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Alternative.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Parsing;

public class AssignmentParsingTests
{
    public static IEnumerable<object[]> ParseAssignment_Correct_DataProvider()
    {
        yield return
        [
            "a := 14.3",
            new AssignNode(
                [new MemberNode("a")],
                [new LiteralNode(14.3, Builtins.Double)]
            )
        ];
        yield return
        [
            "a[1] := 14",
            new AssignNode(
                [new IndexerNode(new MemberNode("a"), new LiteralNode(1, Builtins.Int))],
                [new LiteralNode(14, Builtins.Int)]
            )
        ];
        yield return
        [
            "a[b] := 5",
            new AssignNode(
                [new IndexerNode(new MemberNode("a"), new MemberNode("b"))],
                [new LiteralNode(5, Builtins.Int)]
            )
        ];
        yield return
        [
            "a[b][c] := d",
            new AssignNode(
                [new IndexerNode(new IndexerNode(new MemberNode("a"), new MemberNode("b")), new MemberNode("c"))],
                [new MemberNode("d")]
            )
        ];
        yield return
        [
            "a, b := 1, 2",
            new AssignNode(
                [new MemberNode("a"), new MemberNode("b")],
                [new LiteralNode(1, Builtins.Int), new LiteralNode(2, Builtins.Int)])
        ];
        yield return
        [
            "a[1], b := c, d",
            new AssignNode(
                [new IndexerNode(new MemberNode("a"), new LiteralNode(1, Builtins.Int)), new MemberNode("b")],
                [new MemberNode("c"), new MemberNode("d")])
        ];
        yield return
        [
            "x, y := \"def\", c",
            new AssignNode(
                [new MemberNode("x"), new MemberNode("y")],
                [new LiteralNode("def", Builtins.String), new MemberNode("c")])
        ];
        yield return
        [
            "a1, a2, a3 := a3, a1, a2",
            new AssignNode(
                [new MemberNode("a1"), new MemberNode("a2"), new MemberNode("a3")],
                [new MemberNode("a3"), new MemberNode("a1"), new MemberNode("a2")])
        ];
        yield return
        [
            "x, y := t",
            new AssignNode(
                [new MemberNode("x"), new MemberNode("y")],
                [new MemberNode("t")])
        ];
        yield return
        [
            "a[1][x] := t, 3",
            new AssignNode(
                [new IndexerNode(new IndexerNode(new MemberNode("a"), new LiteralNode(1, Builtins.Int)), new MemberNode("x"))],
                [new MemberNode("t"), new LiteralNode(3, Builtins.Int)])
        ];
        yield return
        [
            "x.y, a.b.c := y.x, 0",
            new AssignNode(
                [
                    new FieldAccessNode(new MemberNode("x"), new FieldNode("y")),
                    new FieldAccessNode(new FieldAccessNode(new MemberNode("a"), new FieldNode("b")), new FieldNode("c"))
                ],
                [
                    new FieldAccessNode(new MemberNode("y"), new FieldNode("x")),
                    new LiteralNode(0, Builtins.Int)
                ])
        ];
        yield return
        [
            "x.y[1].z[0][2] := 1",
            new AssignNode(
                [new IndexerNode(
                    new IndexerNode(
                        new FieldAccessNode(
                            new IndexerNode(
                                new FieldAccessNode(
                                    new MemberNode("x"),
                                    new FieldNode("y")),
                                new LiteralNode(1, Builtins.Int)),
                            new FieldNode("z")),
                        new LiteralNode(0, Builtins.Int)), 
                    new LiteralNode(2, Builtins.Int))
                ],
                [new LiteralNode(1, Builtins.Int)])
        ];
    }
    
    [Theory]
    [MemberData(nameof(ParseAssignment_Correct_DataProvider))]
    public void ParseAssignment_Correct(string code, NodeBase ast)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseAssignment(context, out var assign);
        context.Exceptions.ShouldBeEmpty();
        parsed.ShouldBe(true);
        assign.ShouldBeEquivalentTo(ast);
    }

    public static IEnumerable<object[]> ParseAssignment_Incorrect_DataProvider()
    {
        yield return ["+", new List<string>()];
        yield return ["fn", new List<string>()];
        
        yield return ["b a", new List<string>{PlampExceptionInfo.ExpectedAssignment().Code}];
        
        yield return ["a", new List<string>{PlampExceptionInfo.ExpectedAssignment().Code}];
        yield return ["a := ", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
        yield return ["a := use", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
        
        yield return ["a[", new List<string>{ PlampExceptionInfo.ExpectedAssignment().Code }];
        yield return ["a[1", new List<string>{ PlampExceptionInfo.IndexerIsNotClosed().Code, PlampExceptionInfo.ExpectedAssignment().Code }];
        yield return ["a[1]", new List<string>{ PlampExceptionInfo.ExpectedAssignment().Code }];
        yield return ["a[1] :=", new List<string>{ PlampExceptionInfo.ExpectedAssignmentSource().Code }];
        yield return ["a[1] := +", new List<string>{ PlampExceptionInfo.ExpectedAssignmentSource().Code }];

        yield return ["1 := a", new List<string>(0)];
        yield return ["a, := c", new List<string>{PlampExceptionInfo.ExpectedAssignmentTarget().Code}];
        yield return ["c := a,", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];
        yield return ["a, ,c := d", new List<string>{PlampExceptionInfo.ExpectedAssignmentTarget().Code}];
        yield return ["a := b, ,d", new List<string>{PlampExceptionInfo.ExpectedAssignmentSource().Code}];

        yield return ["x. := 1", new List<string> { PlampExceptionInfo.ExpectedAssignment().Code }];
        yield return ["x.y.", new List<string> { PlampExceptionInfo.ExpectedFieldName().Code, PlampExceptionInfo.ExpectedAssignment().Code }];
    }
    
    [Theory]
    [MemberData(nameof(ParseAssignment_Incorrect_DataProvider))]
    public void ParseAssignment_Incorrect(string code, List<string> errorCodes)
    {
        var fixture = new Fixture();
        fixture.Customizations.Add(new ParserContextCustomization(code));
        var context = fixture.Create<ParsingContext>();
        var parsed = Parser.TryParseAssignment(context, out _);
        parsed.ShouldBe(false);
        context.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errorCodes);
    }
}