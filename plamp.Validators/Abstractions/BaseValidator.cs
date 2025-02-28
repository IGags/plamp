using System;
using plamp.Ast;
using plamp.Ast.Node;
using plamp.Validators.Models;

namespace plamp.Validators.Abstractions;

public abstract class BaseValidator : BaseVisitor
{
    private static readonly NotImplementedException NotImplementedEx 
        = new("NotImplementedException");
    
    public abstract ValidationResult Validate(ValidationContext context);

    //No need to override further
    public sealed override void Visit(NodeBase node) => throw NotImplementedEx;
}