using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    private readonly Dictionary<ITypeBuilderInfo, TypedefNode> _types = [];

    private readonly Dictionary<IFnBuilderInfo, FuncNode> _funcs = [];

    private readonly Dictionary<IFieldBuilderInfo, FieldDefNode> _fields = [];

    /// <inheritdoc cref="ISymTableBuilder.ModuleName" />
    public string ModuleName { get; set; } = "";

    /// <inheritdoc />
    public ITypeBuilderInfo DefineType(TypedefNode typeNode, GenericDefinitionNode[]? generics = null)
    {
        var type = generics is { Length: > 0 } 
            ? new TypeBuilder(typeNode.Name.Value, generics, this) 
            : new TypeBuilder(typeNode.Name.Value, this);
        
        _types.Add(type, typeNode);
        return type;
    }

    /// <inheritdoc />
    public IReadOnlyList<ITypeBuilderInfo> ListTypes() => _types.Keys.ToList();

    /// <inheritdoc />
    public IFnBuilderInfo DefineFunc(FuncNode fnNode)
    {
        var retType = fnNode.ReturnType.TypeInfo;
        if (retType == null) throw new InvalidOperationException();
        if (fnNode.ParameterList.Any(x => x.Type.TypeInfo == null)) throw new InvalidOperationException();

        var args = fnNode.ParameterList.Select(x => new EmptyArgInfo(x.Name.Value, x.Type.TypeInfo!));
        
        var func = new EmptyFuncInfo(fnNode.FuncName.Value, args.ToList(), retType, this);
        _funcs.Add(func, fnNode);
        return func;
    }

    /// <inheritdoc />
    public IReadOnlyList<IFnBuilderInfo> ListFuncs() => _funcs.Keys.ToList();

    /// <inheritdoc />
    public ITypeInfo? FindType(string name, int genericsCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(genericsCount, 0);
        if (name == "") return null;
        name = genericsCount == 0 ? name : $"{name}`{genericsCount}";
        return _types.Keys.FirstOrDefault(x => x.DefinitionName == name);
    }

    /// <inheritdoc />
    public IReadOnlyList<IFnInfo> FindFuncs(string name) => _funcs.Keys.Where(x => x.Name == name).ToList();
    
    /// <inheritdoc />
    public bool TryGetDefinition(ITypeBuilderInfo info, [NotNullWhen(true)] out TypedefNode? defNode)
    {
        return _types.TryGetValue(info, out defNode);
    }

    /// <inheritdoc />
    public bool TryGetInfo(TypedefNode node, [NotNullWhen(true)] out ITypeBuilderInfo? typeInfo)
    {
        typeInfo = _types.FirstOrDefault(x => x.Value == node).Key;
        return typeInfo != null;
    }

    /// <inheritdoc />
    public bool TryGetDefinition(IFieldBuilderInfo info, [NotNullWhen(true)] out FieldDefNode? defNode)
    {
        return _fields.TryGetValue(info, out defNode);
    }

    /// <inheritdoc />
    public bool TryGetDefinition(IFnBuilderInfo info, [NotNullWhen(true)] out FuncNode? defNode)
    {
        return _funcs.TryGetValue(info, out defNode);
    }

    internal void AddField(IFieldBuilderInfo fldInfo, FieldDefNode node)
    {
        _fields.Add(fldInfo, node);
    }
}