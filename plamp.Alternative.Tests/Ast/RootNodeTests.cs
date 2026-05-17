using plamp.Abstractions.Ast.Node.Definitions;
using plamp.Abstractions.Ast.Node.Definitions.Type;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using Shouldly;
using Xunit;

namespace plamp.Alternative.Tests.Ast;

/// <summary>
/// Проверяет базовые операции корневого узла AST
/// </summary>
public class RootNodeTests
{
    /// <summary>
    /// Проверяет, что корневой узел заменяет объявление модуля по старому дочернему узлу
    /// </summary>
    [Fact]
    public void ReplaceChild_WithModuleDefinition_ReplacesModuleName()
    {
        var oldModule = new ModuleDefinitionNode("old");
        var newModule = new ModuleDefinitionNode("new");
        var root = new RootNode([], oldModule, [], []);

        root.ReplaceChild(oldModule, newModule);

        root.ModuleName.ShouldBeSameAs(newModule);
    }

    /// <summary>
    /// Проверяет, что корневой узел заменяет объявление типа на новый дочерний узел
    /// </summary>
    [Fact]
    public void ReplaceChild_WithTypedef_ReplacesType()
    {
        var oldType = new TypedefNode(new TypedefNameNode("Old"), [], []);
        var newType = new TypedefNode(new TypedefNameNode("New"), [], []);
        var root = new RootNode([], null, [], [oldType]);

        root.ReplaceChild(oldType, newType);

        root.Types.ShouldBe([newType]);
    }
}