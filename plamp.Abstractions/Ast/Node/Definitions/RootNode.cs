using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type;

namespace plamp.Abstractions.Ast.Node.Definitions;

/// <summary>
/// Корневой узел любого AST, который генерирует парсер на основании кодового файла
/// </summary>
/// <param name="imports">Список импортов других модулей</param>
/// <param name="moduleName">Имя текущего модуля</param>
/// <param name="functions">Список объявлений функций текущего модуля</param>
public class RootNode(List<ImportNode> imports, ModuleDefinitionNode? moduleName, List<FuncNode> functions) : NodeBase
{
    /// <summary>
    /// Список импортов других модулей
    /// </summary>
    public IReadOnlyList<ImportNode> Imports => imports;

    /// <summary>
    /// Имя текущего модуля
    /// </summary>
    public ModuleDefinitionNode? ModuleName { get; private set; } = moduleName;

    /// <summary>
    /// Список объявлений функций текущего модуля
    /// </summary>
    public IReadOnlyList<FuncNode> Functions => functions;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit()
    {
        foreach (var import in imports)
        {
            yield return import;
        }

        if(ModuleName != null) yield return ModuleName;
        
        foreach (var func in functions)
        {
            yield return func;
        }
    }

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        int ix;
        if (newChild is ImportNode importChild
            && child is ImportNode oldImport
            && (ix = imports.IndexOf(oldImport)) != -1)
        {
            imports[ix] = importChild;
        }
        else if (newChild is ModuleDefinitionNode moduleDefChild && moduleDefChild == ModuleName)
        {
            ModuleName = moduleDefChild;
        }
        else if (newChild is FuncNode defChild
                 && child is FuncNode oldDef
                 && (ix = functions.IndexOf(oldDef)) != -1)
        {
            functions[ix] = defChild;
        }
    }
}