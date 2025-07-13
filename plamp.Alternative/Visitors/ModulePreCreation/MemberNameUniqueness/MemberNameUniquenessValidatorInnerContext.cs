using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;

public class MemberNameUniquenessValidatorInnerContext(BaseVisitorContext other) : PreCreationContext(other)
{
    public Dictionary<string, List<NodeBase>> Members { get; } = [];
}