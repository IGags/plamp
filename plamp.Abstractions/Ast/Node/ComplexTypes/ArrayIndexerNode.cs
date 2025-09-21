using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

public class ArrayIndexerNode(NodeBase indexMember) : NodeBase
{
    private NodeBase _indexMember = indexMember;
    public NodeBase IndexMember => _indexMember;

    public override IEnumerable<NodeBase> Visit() => [IndexMember];

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (_indexMember == child)
        {
            _indexMember = newChild;
        }
    }
}