using AutoFixture;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.TypeInference;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference;

public class FuncDefinitionInferenceTests
{
    [Fact]
    public void HandleFuncWithDuplicateArgs_Correct()
    {
        const string code = """
                            fn nop(a :int, b :string) {}
                            """;
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var context = fixture.Create<ParsingContext>();
        var result = Parser.TryParseTopLevel(context, out var expression);
        result.ShouldBe(true);
        var visitor = new TypeInferenceWeaver();
        var preCreation = new PreCreationContext(context.TranslationTable, new SymbolTable("mod", []));
        var weaveResult = Should.NotThrow(() => visitor.WeaveDiffs(expression!, preCreation));
        weaveResult.Exceptions.ShouldBeEmpty();
    }
}