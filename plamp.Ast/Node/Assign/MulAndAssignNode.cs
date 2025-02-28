using System;
using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

public class MulAndAssignNode : BaseAssignNode
{
    public MulAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}