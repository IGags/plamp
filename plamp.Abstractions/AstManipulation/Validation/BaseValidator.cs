using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Validation;

public abstract class BaseValidator<TOuterContext, TInnerContext> 
    : BaseVisitor<TInnerContext>, IValidator<TOuterContext>
    where TOuterContext : BaseVisitorContext
    where TInnerContext : BaseVisitorContext
{
    public virtual TOuterContext Validate(NodeBase ast, TOuterContext context)
    {
        var innerContext = CreateInnerContext(context);
        VisitInternal(ast, innerContext);
        var result = MapInnerToOuter(context, innerContext);
        return result;
    }

    protected sealed override VisitResult VisitInternal(NodeBase node, TInnerContext context)
    {
        return base.VisitInternal(node, context);
    }

    protected abstract TInnerContext CreateInnerContext(TOuterContext context);
    
    protected abstract TOuterContext MapInnerToOuter(TOuterContext outerContext, TInnerContext innerContext);
}