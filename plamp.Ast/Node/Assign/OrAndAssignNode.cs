using System;
using System.Collections.Generic;
using plamp.Ast.Node.Binary;

namespace plamp.Ast.Node.Assign;

public class OrAndAssignNode : BaseAssignNode
{
    public OrAndAssignNode(NodeBase left, NodeBase right) : base(left, right)
    {
    }
}