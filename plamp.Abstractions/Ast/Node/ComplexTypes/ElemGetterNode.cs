using System;
using System.Collections.Generic;

namespace plamp.Abstractions.Ast.Node.ComplexTypes;

public class ElemGetterNode(NodeBase from, ArrayIndexerNode arrayIndexer) : NodeBase
{
    public Type? ItemType { get; private set; }
    
    public NodeBase From { get; private set; } = from;
    
    public ArrayIndexerNode ArrayIndexer {get; private set; } = arrayIndexer;

    public void SetItemType(Type type)
    {
        ItemType = type;
    }
    
    public override IEnumerable<NodeBase> Visit()
    {
        yield return From;
        yield return ArrayIndexer;
    }

    public override void ReplaceChild(NodeBase child, NodeBase newChild)
    {
        if (child == From) From = newChild;
        if (child == ArrayIndexer && newChild is ArrayIndexerNode newIndexer) ArrayIndexer = newIndexer;
    }
}