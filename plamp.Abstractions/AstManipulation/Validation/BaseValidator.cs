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
        VisitNodeBase(ast, innerContext, null);
        var result = MapInnerToOuter(context, innerContext);
        return result;
    }

    protected sealed override VisitResult VisitNodeBase(NodeBase node, TInnerContext context, NodeBase? parent)
    {
        return base.VisitNodeBase(node, context, parent);
    }

    protected abstract TInnerContext CreateInnerContext(TOuterContext context);
    
    protected abstract TOuterContext MapInnerToOuter(TOuterContext outerContext, TInnerContext innerContext);

    protected void SetExceptionToSymbol(NodeBase node, PlampExceptionRecord record, TInnerContext context)
    {
        var exception = context.SymbolTable.SetExceptionToNode(node, record, context.FileName);
        context.Exceptions.Add(exception);
    }
}