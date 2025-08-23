using AutoFixture;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.DuplicateArgumentName;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class DuplicateArgumentNameTests
{
    [Fact]
    public void DuplicateArgumentName_ReturnException()
    {
        const string code = "fn dup(int a, string a);";
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new DuplicateArgumentNameValidator();
        var preCreation = new PreCreationContext(context.FileName, context.SymbolTable);
        var weaveResult = visitor.Validate(expression!, preCreation);
        weaveResult.Exceptions.ShouldSatisfyAllConditions(
            x => x.ForEach(y => y.Code.ShouldBe(PlampExceptionInfo.DuplicateParameterName().Code)),
            x => x.Count.ShouldBe(2));
    }
}