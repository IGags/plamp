using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class BodyNode : NodeBase
{
    private readonly List<NodeBase> _expressionList;

    public IReadOnlyList<NodeBase> ExpressionList => _expressionList;
    
    public BodyNode(List<NodeBase> expressionList)
    {
        _expressionList = expressionList;
    }

    public override IEnumerable<NodeBase> Visit()
    {
        return ExpressionList;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        var index = _expressionList.IndexOf(child);
        if(index < 0) return;
        _expressionList[index] = newChild;
    }
}