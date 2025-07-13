using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;
using plamp.Abstractions.AstManipulation;

namespace plamp.Alternative.Visitors.ModulePreCreation.ModuleName;

public class ModuleNameValidatorContext(BaseVisitorContext other) : PreCreationContext(other)
{
    public Dictionary<string, List<NodeBase>> Members { get; } = [];
}