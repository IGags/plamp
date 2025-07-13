using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative;

public class SymbolTable : ISymbolTable
{
    private readonly Dictionary<NodeBase, KeyValuePair<FilePosition, FilePosition>> _symbols = [];

    public void AddSymbol(NodeBase symbol, FilePosition start, FilePosition end)
    {
        if (start > end) throw new ArgumentException("Start position cannot be less than one");
        _symbols.Add(symbol, new KeyValuePair<FilePosition, FilePosition>(start, end));
    }

    public PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord, string fileName)
    {
        if (TryGetSymbol(node, out var position))
        {
            return new PlampException(exceptionRecord, position.Key, position.Value, fileName);
        }
        throw new ArgumentException("Node not found in symbol table");
    }

    public PlampException SetExceptionToNodeRange(List<NodeBase> nodes, PlampExceptionRecord exceptionRecord, string fileName)
    {
        if (nodes.Count == 0) throw new ArgumentException("Empty node list");
        var min = new FilePosition(int.MaxValue, int.MaxValue);
        var max = new FilePosition(int.MinValue, int.MinValue);
        foreach (var node in nodes)
        {
            if (!TryGetSymbol(node, out var position)) throw new ArgumentException("Node not found in symbol table");
            if (position.Key < min) min = position.Key;
            if (position.Value > max) max = position.Value;
        }
        
        return new PlampException(exceptionRecord, min, max, fileName);
    }

    public bool TryGetSymbol(NodeBase symbol, out KeyValuePair<FilePosition, FilePosition> pair)
    {
        return _symbols.TryGetValue(symbol, out pair);
    }
}