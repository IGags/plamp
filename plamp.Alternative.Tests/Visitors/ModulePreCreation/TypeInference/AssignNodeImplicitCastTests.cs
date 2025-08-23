using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Assign;
using plamp.Abstractions.Ast.Node.Body;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class AssignNodeImplicitCastTests
{
    [Fact]
    public void AssignIntToDoubleDefinition_ReturnsCorrect()
    {
        const string code = "double a := 1i";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        expression.ShouldBeOfType<AssignNode>()
            .Right.ShouldBeOfType<CastNode>()
            .ShouldSatisfyAllConditions(
                x => x.FromType.ShouldBe(typeof(int)),
                x => x.ToType.ShouldBeOfType<TypeNode>().Symbol.ShouldBe(typeof(double)));
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void AssignIntToDoubleMember_ReturnsCorrect()
    {
        const string code = """
                            {
                                double a;
                                a := 1i;
                            }
                            """;
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseBody(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        expression.ShouldBeOfType<BodyNode>()
            .ShouldSatisfyAllConditions(
                x => x.ExpressionList.Count.ShouldBe(2),
                x => x.ExpressionList[1]
                    .ShouldBeOfType<AssignNode>()
                    .Right.ShouldBeOfType<CastNode>()
                    .ShouldSatisfyAllConditions(
                        y => y.FromType.ShouldBe(typeof(int)),
                        y => y.ToType.ShouldBeOfType<TypeNode>().Symbol.ShouldBe(typeof(double))));
        weaveResult.Exceptions.ShouldBeEmpty();
    }

    [Fact]
    public void AssignDoubleToInt_ReturnsException()
    {
        const string code = "int a := 1d";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseStatement(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
    }
    
    [Fact]
    public void AssignDoubleToIntDefinition_ReturnsException()
    {
        const string code = """
                            {
                                int a;
                                a := 1d;
                            }
                            """;
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseBody(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.WeaveDiffs(expression!, preCreation);
        weaveResult.Exceptions.ShouldHaveSingleItem().Code.ShouldBe(PlampExceptionInfo.CannotAssign().Code);
    }
}