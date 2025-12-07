using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Alternative.Visitors.ModulePreCreation;
using plamp.Alternative.Visitors.ModulePreCreation.ModuleName;
using plamp.Intrinsics;
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
        var symbolTable = SymbolTableInitHelper.CreateEmptyTable();
        var context = new SymbolTableBuildingContext(translationTable, [RuntimeSymbols.SymbolTable], symbolTable);
        var visitor = new ModuleNameValidator();
        var resultContext = visitor.Validate(tree, context);
        Assert.Empty(context.Exceptions);
        Assert.Equal(moduleName, resultContext.CurrentModuleTable.ModuleName);
    }

    [Fact]
    public void ModuleDoesNotExistsInTree_ReturnsException()
    {
        var symbols = new TranslationTable();
        var tree = new RootNode([], null, [], []);
        symbols.AddSymbol(tree, new FilePosition(-1, 0, ""));
        var visitor = new ModuleNameValidator();
        var symbolTable = SymbolTableInitHelper.CreateEmptyTable();
        var nameBeforeVisit = symbolTable.ModuleName;
        var context = new SymbolTableBuildingContext(symbols, [RuntimeSymbols.SymbolTable], symbolTable);
        var resultContext = visitor.Validate(tree, context);
        Assert.Single(context.Exceptions);
        Assert.Equal(nameBeforeVisit, resultContext.CurrentModuleTable.ModuleName);
    }
}