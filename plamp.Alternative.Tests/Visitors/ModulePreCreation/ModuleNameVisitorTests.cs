using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.ModuleName;
using Xunit;

namespace plamp.Alternative.Tests.Visitors.ModulePreCreation;

public class ModuleNameVisitorTests
{
    
    [Fact]
    public void ModuleExistsInTree_ReturnsNoException()
    {
        var translationTable = new TranslationTable();
        var moduleName = "aaa";
        var module = new ModuleDefinitionNode(moduleName);
        translationTable.AddSymbol(module, new FilePosition(0, 3, ""));
        var tree = new RootNode([], module, [], []);
        translationTable.AddSymbol(tree, new FilePosition(-1, 0, ""));
        var context = new PreCreationContext(translationTable, new SymbolTable("%UNDEFINED%", []));
        var visitor = new ModuleNameValidator();
        var resultContext = visitor.Validate(tree, context);
        Assert.Empty(context.Exceptions);
        Assert.Equal(moduleName, resultContext.SymbolTable.ModuleName);
    }

    [Fact]
    public void ModuleDoesNotExistsInTree_ReturnsException()
    {
        var symbols = new TranslationTable();
        var tree = new RootNode([], null, [], []);
        symbols.AddSymbol(tree, new FilePosition(-1, 0, ""));
        var visitor = new ModuleNameValidator();
        const string nameShould = "%UNDEFINED%";
        var context = new PreCreationContext(symbols, new SymbolTable(nameShould, []));
        var resultContext = visitor.Validate(tree, context);
        Assert.Single(context.Exceptions);
        Assert.Equal(nameShould, resultContext.SymbolTable.ModuleName);
    }
}