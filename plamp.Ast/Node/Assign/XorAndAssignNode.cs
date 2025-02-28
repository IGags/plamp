using System;
using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

public class XorAndAssignNode : BaseAssignNode
{
    public XorAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}