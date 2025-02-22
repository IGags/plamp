using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

public class SubAndAssignNode : BaseBinaryNode
{
    public SubAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}