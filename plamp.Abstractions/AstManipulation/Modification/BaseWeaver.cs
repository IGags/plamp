using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Modification;

public abstract class BaseWeaver<TOuterContext, TInnerContext> 
    : BaseVisitor<TInnerContext>, IWeaver<TOuterContext> 
    where TOuterContext : BaseVisitorContext 
    where TInnerContext : BaseVisitorContext
{
    protected Dictionary<NodeBase, NodeBase> ReplacementDict { get; } = [];
    
    public virtual TOuterContext WeaveDiffs(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitInternal(ast, innerContext);
        return MapInnerToOuter(innerContext, context);
    }

    protected sealed override VisitResult VisitInternal(NodeBase node, TInnerContext context)
    {
        return base.VisitInternal(node, context);
    }

    protected abstract TInnerContext CreateInnerContext(TOuterContext context);

    protected abstract TOuterContext MapInnerToOuter(TInnerContext innerContext, TOuterContext outerContext);

    protected void Replace(NodeBase from, NodeBase to, TInnerContext context)
    {
    }
}