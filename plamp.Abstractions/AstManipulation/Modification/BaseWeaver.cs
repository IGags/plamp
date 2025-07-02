using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Abstractions.AstManipulation.Modification;

public abstract class BaseWeaver<TOuterContext, TInnerContext> : BaseVisitor<TInnerContext>, IWeaver<TOuterContext>
{
    protected Dictionary<NodeBase, NodeBase> ReplacementDict { get; } = [];
    
    public virtual WeaveResult WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitInternal(ast, innerContext);
        return CreateWeaveResult(innerContext, context);
    }

    protected abstract TInnerContext CreateInnerContext(TOuterContext context);

    protected virtual WeaveResult CreateWeaveResult(TInnerContext innerContext, TOuterContext outerContext)
    {
        return new WeaveResult() { NodeDiffDictionary = ReplacementDict };
    }

    protected void Replace(NodeBase from, NodeBase to)
    {
        ReplacementDict.Add(from, to);
    }

    
}