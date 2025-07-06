using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative;

public class SymbolTable
{
    private readonly Dictionary<NodeBase, KeyValuePair<FilePosition, FilePosition>> _symbols = [];
    
    public void AddSymbol(NodeBase symbol, FilePosition start, FilePosition end) 
        => _symbols[symbol] = new KeyValuePair<FilePosition, FilePosition>(start, end);

    public void ReplaceSymbol(NodeBase oldNode, NodeBase newNode)
    {
        if (_symbols.Remove(oldNode, out var pair))
        {
            _symbols[newNode] = pair;
        }
    }

    public KeyValuePair<FilePosition, FilePosition> GetSymbol(NodeBase symbol) => _symbols[symbol];
    
    public PlampException CreateExceptionForSymbol(
        NodeBase symbol, 
        PlampExceptionRecord record,
        string fileName)
    {
        if (_symbols.TryGetValue(symbol, out var position))
        {
            return new PlampException(record, position.Key, position.Value, fileName, null);
        }
        throw new ArgumentException("Node not found in symbol table");
    }
}