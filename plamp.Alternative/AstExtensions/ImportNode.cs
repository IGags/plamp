using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.AstExtensions;

public class ImportNode(string moduleName, List<ImportItemNode> importedItems) : NodeBase
{
    public IReadOnlyList<ImportItemNode> ImportedItems { get; } = importedItems;

    public string ModuleName { get; } = moduleName;
    
    public override IEnumerable<NodeBase> Visit()
    {
        return importedItems;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        throw new System.NotImplementedException();
    }
}