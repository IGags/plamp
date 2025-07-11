using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Modification.Modlels;

namespace plamp.Abstractions.AstManipulation.Modification;

public abstract class BaseWeaver<TOuterContext, TInnerContext, TResult> 
    : BaseVisitor<TInnerContext>, IWeaver<TOuterContext, TResult>
{
    protected Dictionary<NodeBase, NodeBase> ReplacementDict { get; } = [];
    
    public virtual TResult WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitInternal(ast, innerContext);
        return CreateWeaveResult(innerContext, context);
    }

    protected sealed override VisitResult VisitInternal(NodeBase node, TInnerContext context)
    {
        return base.VisitInternal(node, context);
    }

    protected abstract TInnerContext CreateInnerContext(TOuterContext context);

    protected abstract TResult CreateWeaveResult(TInnerContext innerContext, TOuterContext outerContext);

    protected void Replace(NodeBase from, NodeBase to)
    {
        ReplacementDict.Add(from, to);
    }

    
}