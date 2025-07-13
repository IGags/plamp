using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Definitions;

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