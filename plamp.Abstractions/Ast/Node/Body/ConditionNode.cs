using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.Body;

public class ConditionNode : NodeBase
{
    public NodeBase Predicate { get; private set; }
    public NodeBase IfClause { get; private set; }
    public NodeBase? ElseClause { get; private set; }

    public ConditionNode(NodeBase predicate, NodeBase ifClause, NodeBase? elseClause)
    {
        Predicate = predicate;
        IfClause = ifClause;
        ElseClause = elseClause;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return Predicate;
        yield return IfClause;
        if (ElseClause != null)
        {
            yield return ElseClause;
        }
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (Predicate == child)
        {
            Predicate = newChild;
        }
        else if(IfClause == child)
        {
            IfClause = newChild;
        }
        else if (ElseClause == child)
        {
            ElseClause = newChild;
        }
    }
}