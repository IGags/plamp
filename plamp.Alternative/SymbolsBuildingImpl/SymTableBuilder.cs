using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

public class SymTableBuilder : ISymTableBuilder, ISymTable
{
    private readonly List<ITypeBuilderInfo> _types = [];

    private readonly List<IFnBuilderInfo> _funcs = [];

    public string ModuleName { get; set; } = "";

    public ITypeBuilderInfo DefineType(TypedefNode typeNode)
    {
        var type = new TypeBuilder(typeNode);
        _types.Add(type);
        return type;
    }

    public List<ITypeBuilderInfo> ListTypes() => _types;

    public IFnBuilderInfo DefineFunc(FuncNode fnNode)
    {
        var func = new EmptyFuncInfo(fnNode);
        _funcs.Add(func);
        return func;
    }

    public List<IFnBuilderInfo> ListFuncs() => _funcs;

    public ITypeInfo? FindType(string name) => _types.FirstOrDefault(x => x.Name == name);

    public IReadOnlyList<IFnInfo> FindFuncs(string name) => _funcs.Where(x => x.Name == name).ToList();
}