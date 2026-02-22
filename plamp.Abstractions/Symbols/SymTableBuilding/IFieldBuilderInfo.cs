using System.Reflection.Emit;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

public interface IFieldBuilderInfo : IFieldInfo
{
    public FieldDefNode Definition { get; }
    
    public FieldBuilder? Field { get; set; }
}