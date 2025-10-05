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
        var symbols = new SymbolTable();
        var moduleName = "aaa";
        var module = new ModuleDefinitionNode(moduleName);
        symbols.AddSymbol(module, new FilePosition(0, 3, ""));
        var tree = new RootNode([], module, []);
        symbols.AddSymbol(tree, new FilePosition(-1, 0, ""));
        var context = new PreCreationContext(symbols);
        var visitor = new ModuleNameValidator();
        var resultContext = visitor.Validate(tree, context);
        Assert.Empty(context.Exceptions);
        Assert.Equal(moduleName, resultContext.ModuleName);
    }

    [Fact]
    public void ModuleDoesNotExistsInTree_ReturnsException()
    {
        var symbols = new SymbolTable();
        var tree = new RootNode([], null, []);
        symbols.AddSymbol(tree, new FilePosition(-1, 0, ""));
        var visitor = new ModuleNameValidator();
        var context = new PreCreationContext(symbols);
        var resultContext = visitor.Validate(tree, context);
        Assert.Single(context.Exceptions);
        Assert.Null(resultContext.ModuleName);
    }
}