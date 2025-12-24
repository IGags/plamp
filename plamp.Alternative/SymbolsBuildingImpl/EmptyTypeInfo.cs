using System;
using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class EmptyTypeInfo(TypedefNode typedefNode) : ITypeInfo
{
    public Type AsType() => throw new NotSupportedException();
    
    public IReadOnlyList<IFieldInfo> Fields { get; } = typedefNode.Fields.Select(x => new EmptyFieldInfo(x)).ToList();

    public string Name => typedefNode.Name.Value;
}