using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.AstManipulation.Validation;

public interface IValidator<TContext> where TContext : BaseVisitorContext
{
    public TContext Validate(NodeBase ast, TContext context);
}