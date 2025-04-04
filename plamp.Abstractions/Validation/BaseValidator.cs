using System;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Validation.Models;

namespace plamp.Abstractions.Validation;

public abstract class BaseValidator<TContext> : BaseVisitor<TContext>, IValidator
{
    //No need to override further
    public sealed override void Visit(NodeBase node, TContext context) => throw new NotImplementedException();
    
    public virtual ValidationResult Validate(ValidationContext context) => throw new NotImplementedException();
}