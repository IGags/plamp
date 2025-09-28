using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

/// <summary>
/// Узел директивы импорта элементов другого модуля
/// </summary>
/// <param name="moduleName">Имя импортируемого модуля</param>
/// <param name="importedItems">Список импортируемых членов модуля с их псевдонимами. Если не указан, то импортируется всё</param>
public class ImportNode(string moduleName, List<ImportItemNode>? importedItems) : NodeBase
{
    /// <summary>
    /// Список импортируемых членов модуля с их псевдонимами. Если не указан, то импортируется всё
    /// </summary>
    public IReadOnlyList<ImportItemNode>? ImportedItems { get; } = importedItems;

    /// <summary>
    /// Имя импортируемого модуля
    /// </summary>
    public string ModuleName { get; } = moduleName;
    
    /// <inheritdoc cref="NodeBase"/>
    public override IEnumerable<NodeBase> Visit() => importedItems ?? [];

    /// <inheritdoc cref="NodeBase"/>
    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if(importedItems == null) return;
        int ix;
        if (child is ImportItemNode oldImportItem
            && newChild is ImportItemNode newImportItem
            && (ix = importedItems.IndexOf(oldImportItem)) != -1)
        {
            importedItems[ix] = newImportItem;
        }
    }
}