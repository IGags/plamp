using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative;

public class SymbolTable
{
    private readonly Dictionary<NodeBase, KeyValuePair<FilePosition, FilePosition>> _symbols = [];
    
    public void AddSymbol(NodeBase symbol, FilePosition start, FilePosition end) 
        => _symbols[symbol] = new KeyValuePair<FilePosition, FilePosition>(start, end);
}