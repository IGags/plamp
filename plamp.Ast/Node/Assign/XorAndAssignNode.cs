using System;
using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

public class XorAndAssignNode : BaseBinaryNode
{
    public XorAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}