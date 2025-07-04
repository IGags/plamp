using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.AstExtensions;

public class ModuleDefinitionNode(string moduleName) : NodeBase
{
    public string ModuleName = moduleName;

    public override IEnumerable<NodeBase> Visit()
    {
        return [];
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        throw new System.NotImplementedException();
    }
}