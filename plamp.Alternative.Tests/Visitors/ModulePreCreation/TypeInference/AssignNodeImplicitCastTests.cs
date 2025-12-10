using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using plamp.Intrinsics;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class AssignNodeImplicitCastTests
{
    [Fact]
    public void AssignIntToDoubleMember_ReturnsCorrect()
    {
        const string code = """
                            {
                                a :double;
                                a := 1i;
                            }
                            """;
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseBody(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        expression.ShouldBeOfType<BodyNode>()
            .ShouldSatisfyAllConditions(
                x => x.ExpressionList.Count.ShouldBe(2),
                x => x.ExpressionList[1]
                    .ShouldBeOfType<AssignNode>()
                    .Sources.ShouldHaveSingleItem()
                    .ShouldBeOfType<CastNode>()
                    .ShouldSatisfyAllConditions(
                        y => y.FromType.ShouldBe(RuntimeSymbols.SymbolTable.Int),
                        y => y.ToType.ShouldBeOfType<TypeNode>().TypedefRef.ShouldBe(RuntimeSymbols.SymbolTable.Double)));
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void AssignDoubleToInt_ReturnsException()
    {
        const string code = """
                            {
                                a :int;
                                a := 1d;
                            }
                            """;
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseMultilineBody(context, out var expressions);
        expressions.ShouldNotBeNull().ExpressionList.Count.ShouldBe(2);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, SymbolTableInitHelper.CreateDefaultTables());
        var weaveResult = visitor.WeaveDiffs(expressions, preCreation);
        weaveResult.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
    }
}