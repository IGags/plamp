using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;

public class FieldInferenceInnerContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public List<FieldDefNode> Fields { get; } = [];
}