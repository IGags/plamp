using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative;

//TODO: Need to fork, for decrease memory consumption
public class SymbolTable : ISymbolTable
{
    private readonly Dictionary<NodeBase, FilePosition> _symbols = [];

    public void AddSymbol(NodeBase symbol, FilePosition position)
    {
        _symbols.Add(symbol, position);
    }

    public PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord)
    {
        if (TryGetSymbol(node, out var position)) return new PlampException(exceptionRecord, position);
        throw new ArgumentException("Node not found in symbol table");
    }

    public bool TryGetSymbol(NodeBase symbol, out FilePosition position) => _symbols.TryGetValue(symbol, out position);
}