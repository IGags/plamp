using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class BinaryArithmeticalExpressionImplicitCastTests
{
    
    
    [Theory]
    public void CreateImplicitCastForArithmeticalBinaryExpression_Correct(string code, NodeBase astShould)
    {
        // var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        // var context = fixture.Create<ParsingContext>();
        // var result = Parser.TryParsePrecedence(context, out var expression);
        // result.ShouldBe(true);
        // var context = 
    }
}