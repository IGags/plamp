using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.Validation.Models;

namespace plamp.Validators.BasicSemanticsValidators;

public record ImpossibleTreeValidatorContext : ValidationContext
{
    public ImpossibleTreeValidatorContext(ValidationContext context) : base(context)
    { }
    
    public HashSet<NodeBase> SkippedBranches { get; init; } = [];
}