using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;

public class FieldInferenceInnerContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public List<FieldDefNode> Fields { get; } = [];
}