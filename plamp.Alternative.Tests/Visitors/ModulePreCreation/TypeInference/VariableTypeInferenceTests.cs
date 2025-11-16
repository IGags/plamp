using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoFixture.Xunit2;
using Moq;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Variable;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class VariableTypeInferenceTests
{
    [Theory, AutoData]
    public void VariableDefinitionInference_ReturnNoExceptions([Frozen]Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void NotExistVariableInference_ReturnsVariableNotExistException([Frozen]Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var exceptionMember = new MemberNode("b");
        var ast = new BodyNode([
            new AssignNode([new MemberNode("a")], [exceptionMember])
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        
        symbolTable.Verify(x => x.SetExceptionToNode(exceptionMember, It.IsAny<PlampExceptionRecord>()), Times.Once);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Theory, AutoData]
    public void CreateAndUseVariableDefinition_ReturnNoException([Frozen]Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            new AssignNode([new MemberNode("b")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void CreateVariableAndAssignOtherType_InvalidOperationException([Frozen]Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var exceptionMember = new AssignNode([new MemberNode("a")], [new LiteralNode("123", typeof(string))]);
        var ast = new BodyNode([
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            exceptionMember
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        
        symbolTable.Verify(x => x.SetExceptionToNode(exceptionMember, It.IsAny<PlampExceptionRecord>()), Times.Once);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code));
    }

    [Theory, AutoData]
    public void CreateVariableBeforeAndGetFromChildScope_ReturnsNoException([Frozen]Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(2, typeof(int))])
            ])
        ]);
        
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void CreateVariableAfterGetFromChildScope_ReturnsDuplicateDefinitionException([Frozen] Mock<ITranslationTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(2, typeof(int))])
            ]),
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))])
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldSatisfyAllConditions(
                y => y.Count.ShouldBe(2),
                y => y[0].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code),
                y => y[1].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code))
        );
    }

    [Theory, AutoData]
    public void CreateVariableInOtherScopeStack_ReturnsNoException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(2, typeof(int))])
            ]),
            new BodyNode(
            [
                new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))])
            ])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void DefineVariableExplicitly_ReturnsNoException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void DefineVariableExplicitlyAndAssign_ReturnsNoException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode(
                [new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))],
                [new LiteralNode(1, typeof(int))])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }


    [Theory, AutoData]
    public void DefineVariableExplicitlyTwice_ReturnsDuplicateDefinitionException(
        [Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a"))
        ]);
        
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldSatisfyAllConditions(
                y => y.Count.ShouldBe(2),
                y => y[0].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code),
                y => y[1].Code.ShouldBe(PlampExceptionInfo.DuplicateVariableDefinition().Code))
            );
    }

    [Theory, AutoData]
    public void DefineVariableAndAssignToOther_ReturnsNoException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new LiteralNode(1, typeof(int))]),
            new AssignNode([new MemberNode("b")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void AssignUndefined_ReturnsException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new MemberNode("b")])
        ]);
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Theory, AutoData]
    public void AssignThemself_ReturnsException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new AssignNode([new MemberNode("a")], [new MemberNode("a")])
        ]);
        SetupExceptionGenerationMock(symbolTable);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.ShouldSatisfyAllConditions(
            x => x.Exceptions.ShouldHaveSingleItem(),
            x => x.Exceptions[0].Code.ShouldBe(PlampExceptionInfo.CannotFindMember().Code));
    }

    [Theory, AutoData]
    public void AssignEmptyDefinition_ReturnsNoException(
        [Frozen] Mock<ITranslationTable> symbolTable,
        TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
            new AssignNode([new MemberNode("b")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    [Theory, AutoData]
    public void AssignThemselfAfterDefinition_ReturnsNoException([Frozen] Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var ast = new BodyNode(
        [
            new VariableDefinitionNode(new TypeNode(new TypeNameNode("int")), new VariableNameNode("a")),
            new AssignNode([new MemberNode("a")], [new MemberNode("a")])
        ]);
        SetupMocksAndAssertCorrect(ast, symbolTable, visitor);
    }

    private void SetupMocksAndAssertCorrect(NodeBase ast, Mock<ITranslationTable> symbolTable, TypeInferenceWeaver visitor)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        var context = new PreCreationContext(symbolTable.Object);
        var result = visitor.WeaveDiffs(ast, context);
        result.Exceptions.ShouldBeEmpty();
    }

    private void SetupExceptionGenerationMock(Mock<ITranslationTable> symbolTable)
    {
        var filePosition = new FilePosition();
        symbolTable.Setup(x => x.TryGetSymbol(It.IsAny<NodeBase>(), out filePosition)).Returns(true);
        symbolTable.Setup(x => x.SetExceptionToNode(It.IsAny<NodeBase>(), It.IsAny<PlampExceptionRecord>()))
            .Returns<NodeBase, PlampExceptionRecord>((_, b) => new PlampException(b, default));
    }

    public static IEnumerable<object[]> AssignMultiple_Correct_DataProvider()
    {
        yield return 
        [
            """
            {
                a, b := 1, "abc";
            }
            """
        ];
        yield return 
        [
            """
            {
                c :int;
                d :double;
                a, b := c, d;
            }
            """
        ];
        yield return
        [
            """
            {
                t := [3]int;
                t[0], t[1], t[2] := 5, 4, 3;
            }
            """
        ];
        yield return
        [
            """
            {
                a, b := 2, 4;
                a, b := b, a;
            }
            """
        ];
    }
    
    [Theory]
    [MemberData(nameof(AssignMultiple_Correct_DataProvider))]
    public void AssignMultiple_Correct(string code)
    {
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization(), new ParserContextCustomization(code) }};
        var parserContext = fixture.Create<ParsingContext>();
        var parserResult = Parser.TryParseMultilineBody(parserContext, out var body);
        parserResult.ShouldBe(true);
        body.ShouldNotBeNull();
        
        var preCreationContext = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        var result = visitor.WeaveDiffs(body, preCreationContext);
        result.Exceptions.ShouldBeEmpty();
    }

    public static IEnumerable<object[]> AssignMultiple_Incorrect_DataProvider()
    {
        yield return
        [
            """
            {
                a := 1, 2;
            }
            """,
            new List<string>{PlampExceptionInfo.AssignSourceAndTargetCountMismatch().Code}
        ];
        yield return
        [
            """
            {
                a, b := 1;
            }
            """,
            new List<string>{PlampExceptionInfo.AssignSourceAndTargetCountMismatch().Code}
        ];
        yield return
        [
            """
            {
                a := print("14");
            }
            """,
            new List<string>{PlampExceptionInfo.CannotAssignNone().Code}
        ];
        yield return
        [
            """
            {
                a, b := b, a;
            }
            """,
            new List<string>{PlampExceptionInfo.CannotFindMember().Code, PlampExceptionInfo.CannotFindMember().Code}
        ];
    }
    
    [Theory]
    [MemberData(nameof(AssignMultiple_Incorrect_DataProvider))]
    public void AssignMultiple_Incorrect(string code, List<string> errors)
    {
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization(), new ParserContextCustomization(code) }};
        var parserContext = fixture.Create<ParsingContext>();
        var parserResult = Parser.TryParseMultilineBody(parserContext, out var body);
        parserResult.ShouldBe(true);
        body.ShouldNotBeNull();
        
        var preCreationContext = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        var result = visitor.WeaveDiffs(body, preCreationContext);
        result.Exceptions.Count.ShouldBe(errors.Count);
        result.Exceptions.Select(x => x.Code).ToList().ShouldBeEquivalentTo(errors);
    }

    public static IEnumerable<object[]> InitDefault_Correct_DataProvider()
    {
        var defType1 = new TypeNode(new TypeNameNode("int"));
        defType1.SetTypeRef(typeof(int));
        yield return
        [
            """
            {
                a, b, c :int;
            }
            """,
            new AssignNode(
                [
                    new VariableDefinitionNode(
                        defType1, 
                        [new VariableNameNode("a"), new VariableNameNode("b"), new VariableNameNode("c")])
                ],
                [
                    new LiteralNode(0, typeof(int))
                ]
            )
        ];

        var defType2 = new TypeNode(new TypeNameNode("string")){ArrayDefinitions = [new ArrayTypeSpecificationNode()]};
        defType2.SetTypeRef(typeof(string[]));
        var callSymbol = typeof(Array).GetMethod(nameof(Array.Empty));
        var infoConstructed = callSymbol!.MakeGenericMethod(typeof(string));
        var callNode = new CallNode(null, new FuncCallNameNode("__FROM_C#__ARRAY::Empty<T>"), []);
        callNode.SetInfo(infoConstructed);
        yield return
        [
            """
            {
                a, b :[]string;
            }
            """,
            new AssignNode(
                [
                    new VariableDefinitionNode(
                        defType2, 
                        [new VariableNameNode("a"), new VariableNameNode("b")])
                ],
                [
                    //HARDCODED NAME.
                    callNode
                ]
            )
        ];
    }

    [Theory]
    [MemberData(nameof(InitDefault_Correct_DataProvider))]
    public void InitDefault_Correct(string code, NodeBase valueShould)
    {
        var fixture = new Fixture() { Customizations = { new ModulePreCreateCustomization(), new ParserContextCustomization(code) }};
        var parserContext = fixture.Create<ParsingContext>();
        var parserResult = Parser.TryParseMultilineBody(parserContext, out var body);
        parserResult.ShouldBe(true);
        body.ShouldNotBeNull();
        
        var preCreationContext = fixture.Create<PreCreationContext>();
        var visitor = new TypeInferenceWeaver();
        var result = visitor.WeaveDiffs(body, preCreationContext);
        result.Exceptions.ShouldBeEmpty();
        var expression = body.ExpressionList.ShouldHaveSingleItem();
        valueShould.ShouldBeEquivalentTo(expression);
    }
}