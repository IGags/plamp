using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

public class ImportNode(string moduleName, List<ImportItemNode>? importedItems) : NodeBase
{
    public IReadOnlyList<ImportItemNode>? ImportedItems { get; } = importedItems;

    public string ModuleName { get; } = moduleName;
    
    public override IEnumerable<NodeBase> Visit()
    {
        if (importedItems == null) return [];
        return importedItems;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        throw new System.NotImplementedException();
    }
}