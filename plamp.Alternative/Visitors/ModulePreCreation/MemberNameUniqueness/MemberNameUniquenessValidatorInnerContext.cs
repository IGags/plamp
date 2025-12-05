using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.Visitors.ModulePreCreation.MemberNameUniqueness;

public class MemberNameUniquenessValidatorInnerContext(PreCreationContext other) : PreCreationContext(other)
{
    public Dictionary<string, List<NodeBase>> Members { get; } = [];
}