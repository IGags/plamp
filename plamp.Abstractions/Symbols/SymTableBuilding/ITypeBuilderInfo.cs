using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

public interface ITypeBuilderInfo : ITypeInfo
{
    public TypedefNode Definition { get; }
    
    public IReadOnlyList<IFieldBuilderInfo> FieldBuilders { get; }

    public void AddField(FieldDefNode defNode);
}