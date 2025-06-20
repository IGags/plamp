using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation.Models;

namespace plamp.Abstractions.AstManipulation.Validation;

public abstract class BaseValidator<TContext> : BaseVisitor<TContext>, IValidator<TContext>
{
    public abstract ValidationResult Validate(NodeBase ast, TContext context);
}