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
    private readonly Dictionary<string, ITypeBuilderInfo> _types = [];
    private readonly Dictionary<ITypeBuilderInfo, TypedefNode> _typeNodeMapping = [];
    
    private readonly Dictionary<string, IFnBuilderInfo> _funcs = [];
    private readonly Dictionary<IFnBuilderInfo, FuncNode> _fnNodeMapping = [];

    /// <inheritdoc cref="ISymTableBuilder.ModuleName" />
    public string ModuleName { get; set; } = "<default>";

    /// <inheritdoc />
    public ITypeBuilderInfo DefineType(TypedefNode typeNode, IGenericParameterBuilder[]? genericParams = null)
    {
        var name = typeNode.Name.Value;
        if (_types.ContainsKey(name) || _funcs.ContainsKey(name)) throw new InvalidOperationException();
        
        var type = genericParams is { Length: > 0 } 
            ? new TypeBuilder(name, genericParams, ModuleName) 
            : new TypeBuilder(name, ModuleName);
        
        _types.Add(name, type);
        _typeNodeMapping.Add(type, typeNode);
        return type;
    }

    /// <inheritdoc />
    public IReadOnlyList<ITypeBuilderInfo> ListTypes() => _types.Values.ToList();

    /// <inheritdoc />
    public IFnBuilderInfo DefineFunc(FuncNode fnNode, IGenericParameterBuilder[]? generics = null)
    {
        generics ??= [];
        
        var retType = fnNode.ReturnType.TypeInfo;
        if (retType == null) throw new InvalidOperationException();
        if (fnNode.ParameterList.Any(x => x.Type.TypeInfo == null)) throw new InvalidOperationException();
        if (_funcs.ContainsKey(fnNode.FuncName.Value) || _types.ContainsKey(fnNode.FuncName.Value)) throw new InvalidOperationException();

        var args = fnNode.ParameterList.Select(x => new BlankArgInfo(x.Name.Value, x.Type.TypeInfo!));
        
        var func = generics.Length == 0 
            ? new BlankFuncInfo(fnNode.FuncName.Value, args.ToList(), retType, ModuleName)
            : new BlankFuncInfo(fnNode.FuncName.Value, args.ToList(), retType, generics, ModuleName);
        _funcs.Add(fnNode.FuncName.Value, func);
        _fnNodeMapping.Add(func, fnNode);
        
        return func;
    }

    public IGenericParameterBuilder CreateGenericParameter(GenericDefinitionNode genericNode) 
        => new GenericParameterBuilder(genericNode.Name.Value, ModuleName);

    /// <inheritdoc />
    public IReadOnlyList<IFnBuilderInfo> ListFuncs() => _funcs.Values.ToList();

    /// <inheritdoc />
    public ITypeInfo? FindType(string name) => name == "" ? null : _types.GetValueOrDefault(name);

    /// <inheritdoc />
    public IFnInfo? FindFunc(string name) => _funcs.GetValueOrDefault(name);

    public bool ContainsSymbol(string name) => _funcs.ContainsKey(name) || _types.ContainsKey(name);

    /// <inheritdoc />
    public bool TryGetDefinition(ITypeBuilderInfo info, [NotNullWhen(true)] out TypedefNode? defNode) => _typeNodeMapping.TryGetValue(info, out defNode);

    /// <inheritdoc />
    public bool TryGetInfo(string name, [NotNullWhen(true)] out ITypeBuilderInfo? typeInfo) => _types.TryGetValue(name, out typeInfo);

    /// <inheritdoc />
    public bool TryGetInfo(string name, [NotNullWhen(true)] out IFnBuilderInfo? fnInfo) => _funcs.TryGetValue(name, out fnInfo);

    /// <inheritdoc />
    public bool TryGetDefinition(IFnBuilderInfo info, [NotNullWhen(true)] out FuncNode? defNode) => _fnNodeMapping.TryGetValue(info, out defNode);
}
