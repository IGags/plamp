using System;
using AutoFixture;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Alternative.Parsing;
using plamp.Alternative.Tests.Parsing;
using plamp.Alternative.Tests.Visitors.ModulePreCreation.TypeInference.Util;
using plamp.Alternative.Visitors.SymbolTableBuilding;

namespace plamp.Alternative.Tests;

public static class CompilationPipelineBuilder
{
    public static ParsingContext CreateParsingContext(string code)
    {
        var fixture = new Fixture() { Customizations = { new ParserContextCustomization(code) } };
        var parserContext = fixture.Create<ParsingContext>();
        return parserContext;
    }
    
    public static (RootNode ast, ParsingContext context) RunParsingPipeline(string code)
    {
        var parserContext = CreateParsingContext(code);
        var ast = Parser.ParseFile(parserContext);
        return (ast, parserContext);
    }

    public static SymbolTableBuildingContext RunSymbolTableBuildingPipeline(
        string code, 
        Func<NodeBase, SymbolTableBuildingContext, SymbolTableBuildingContext>[] visitors)
    {
        var (ast, context) = RunParsingPipeline(code);
        var fixture = new Fixture() { Customizations = { new SymbolTableBuildingContextCustomization([], context.TranslationTable) } };
        var symbolTableBuildingContext = fixture.Create<SymbolTableBuildingContext>();
        symbolTableBuildingContext.Exceptions.AddRange(context.Exceptions);
        foreach (var visitor in visitors)
        {
            symbolTableBuildingContext = visitor(ast, symbolTableBuildingContext);
        }

        return symbolTableBuildingContext;
    }
}