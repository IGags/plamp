using System;
using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

public class OrAndAssignNode : BaseBinaryNode
{
    public OrAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}