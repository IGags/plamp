using System.Collections.Generic;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.FieldDefInference;

public class FieldInferenceInnerContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public IReadOnlyList<ITypeInfo>? TypeGenericList { get; set; }
}