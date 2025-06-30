using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation.Models;

namespace plamp.Abstractions.AstManipulation.Validation;

public abstract class BaseValidator<TOuterContext, TInnerContext> : BaseVisitor<TInnerContext>, IValidator<TOuterContext>
{
    public virtual ValidationResult Validate(NodeBase ast, TOuterContext context)
    {
        var innerContext = MapContext(context);
        VisitInternal(ast, innerContext);
        var result = CreateResult(context, innerContext);
        return result;
    }

    protected abstract TInnerContext MapContext(TOuterContext context);
    
    protected abstract ValidationResult CreateResult(TOuterContext outerContext, TInnerContext innerContext);
}