using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;

namespace plamp.Abstractions.Symbols.SymTableBuilding;

public interface ISymTableBuilder
{
    public string ModuleName { get; set; }
        
    public ITypeBuilderInfo DefineType(TypedefNode typeNode);
    
    public List<ITypeBuilderInfo> ListTypes();

    public IFnInfo DefineFunc(FuncNode fnNode);

    public List<IFnInfo> ListFuncs();
}