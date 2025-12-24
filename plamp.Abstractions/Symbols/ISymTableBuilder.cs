using System.Collections.Generic;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;

namespace plamp.Abstractions.Symbols;

public interface ISymTableBuilder
{
    public string ModuleName { get; set; }
        
    public ITypeInfo DefineType(TypedefNode typeNode);
    
    public List<ITypeInfo> ListTypes();

    public IFnInfo DefineFunc(FuncNode fnNode);

    public List<IFnInfo> ListFuncs();
}