using System.Collections.Generic;
using System.Linq;
using plamp.Abstractions.Ast.Node.ComplexTypes;
using plamp.Abstractions.Ast.Node.Definitions.Func;
using plamp.Abstractions.Ast.Node.Definitions.Type.Definition;
using plamp.Abstractions.Symbols.SymTable;
using plamp.Abstractions.Symbols.SymTableBuilding;

namespace plamp.Alternative.SymbolsBuildingImpl;

/// <inheritdoc cref="ISymTableBuilder"/>
public class SymTableBuilder : ISymTableBuilder, ISymTable
{
    private readonly List<ITypeBuilderInfo> _types = [];

    private readonly List<IFnBuilderInfo> _funcs = [];

    /// <inheritdoc cref="ISymTableBuilder.ModuleName" />
    public string ModuleName { get; set; } = "";

    /// <inheritdoc />
    public ITypeBuilderInfo DefineType(TypedefNode typeNode, GenericDefinitionNode[]? generics = null)
    {
        var type = generics is { Length: > 0 } ? new TypeBuilder(typeNode, generics) : new TypeBuilder(typeNode);
        
        _types.Add(type);
        return type;
    }

    /// <inheritdoc />
    public IReadOnlyList<ITypeBuilderInfo> ListTypes() => _types;

    /// <inheritdoc />
    public IFnBuilderInfo DefineFunc(FuncNode fnNode)
    {
        var func = new EmptyFuncInfo(fnNode);
        _funcs.Add(func);
        return func;
    }

    /// <inheritdoc />
    public IReadOnlyList<IFnBuilderInfo> ListFuncs() => _funcs;

    /// <inheritdoc />
    public ITypeInfo? FindType(string name) => _types.FirstOrDefault(x => x.Name == name);

    /// <inheritdoc />
    public IReadOnlyList<IFnInfo> FindFuncs(string name) => _funcs.Where(x => x.Name == name).ToList();
}