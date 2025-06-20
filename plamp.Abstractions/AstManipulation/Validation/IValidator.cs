using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation.Validation.Models;

namespace plamp.Abstractions.AstManipulation.Validation;

public interface IValidator<in TContext>
{
    public ValidationResult Validate(NodeBase ast, TContext context);
}