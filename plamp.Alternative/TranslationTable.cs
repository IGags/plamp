using System;
using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Alternative;

public class TranslationTable : ITranslationTable
{
    private readonly ITranslationTable? _parent;
    
    private readonly Dictionary<NodeBase, FilePosition> _symbols = [];

    public TranslationTable() { }

    private TranslationTable(ITranslationTable parent)
    {
        _parent = parent;
    }
    
    public void AddSymbol(NodeBase symbol, FilePosition position)
    {
        const string errorMessage = "Element already exists in table";
        if (_parent?.TryGetSymbol(symbol, out _) ?? false) throw new ArgumentException(errorMessage);
        try
        {
            _symbols.Add(symbol, position);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException(errorMessage);
        }
    }

    public ITranslationTable Fork() => new TranslationTable(this);

    public void Merge(ITranslationTable child)
    {
        const string errorMessage = "Child table does not forked from this table";
        if (child is not TranslationTable translation) throw new InvalidOperationException(errorMessage);
        if (translation._parent != this)               throw new InvalidOperationException(errorMessage);
        
        foreach (var pair in translation._symbols)
        {
            _symbols.Add(pair.Key, pair.Value);
        }
    }

    public PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord)
    {
        if (TryGetSymbol(node, out var position)) return new PlampException(exceptionRecord, position);
        throw new ArgumentException("Node not found in symbol table");
    }

    public bool TryGetSymbol(NodeBase symbol, out FilePosition position)
    {
        if (_symbols.TryGetValue(symbol, out position)) return true;
        return _parent != null && _parent.TryGetSymbol(symbol, out position);
    }
}