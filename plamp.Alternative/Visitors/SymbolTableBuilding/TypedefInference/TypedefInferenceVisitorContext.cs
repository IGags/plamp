using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.TypedefInference;

public class TypedefInferenceVisitorContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public List<TypedefNode> Types { get; } = [];
}