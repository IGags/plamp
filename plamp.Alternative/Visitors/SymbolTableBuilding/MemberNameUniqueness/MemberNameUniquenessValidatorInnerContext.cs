using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.MemberNameUniqueness;

public class MemberNameUniquenessValidatorInnerContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public Dictionary<string, List<NodeBase>> Members { get; } = [];
}