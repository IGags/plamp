using System;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyArgInfo(ParameterNode parameterNode) : IArgInfo
{
    public string Name => parameterNode.Name.Value;

    public ITypeInfo Type => parameterNode.Type.TypeInfo ?? throw new NullReferenceException();
}