using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Alternative.Visitors.SymbolTableBuilding.MemberNameUniqueness;

public class MemberNameUniquenessValidatorInnerContext(SymbolTableBuildingContext other) : SymbolTableBuildingContext(other)
{
    public List<FuncNameNode> Funcs { get; } = [];

    public List<TypedefNameNode> Types { get; } = [];
}