using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Compiler.Util;

public class CompositeSymbolTable : ISymbolTable
{
    private readonly IReadOnlyList<ISymbolTable> _symbolTables;

    public CompositeSymbolTable(IReadOnlyList<ISymbolTable> symbolTables)
    {
        _symbolTables = symbolTables;
    }
    
    public PlampException SetExceptionToNodeAndChildren(PlampExceptionRecord exceptionRecord, NodeBase node, string fileName,
        AssemblyName assemblyName)
    {        
        var table = GetTableOrThrow(node);
        return table.SetExceptionToNodeAndChildren(exceptionRecord, node, fileName, assemblyName);
    }

    public PlampException SetExceptionToNodeWithoutChildren(PlampExceptionRecord exceptionRecord, NodeBase node, string fileName,
        AssemblyName assemblyName)
    {
        var table = GetTableOrThrow(node);
        return table.SetExceptionToNodeWithoutChildren(exceptionRecord, node, fileName, assemblyName);
    }

    public List<PlampException> SetExceptionToChildren(PlampExceptionRecord exceptionRecord, NodeBase node, string fileName,
        AssemblyName assemblyName)
    {
        var table = GetTableOrThrow(node);
        return table.SetExceptionToChildren(exceptionRecord, node, fileName, assemblyName);
    }

    public bool Contains(NodeBase node)
    {
        return _symbolTables.FirstOrDefault(x => x.Contains(node)) != null;
    }

    public bool TryGetChildren(NodeBase node, out IReadOnlyList<NodeBase> children)
    {
        children = null;
        foreach (var table in _symbolTables)
        {
            if (table.TryGetChildren(node, out children))
            {
                return true;
            }
        }
        return false;
    }

    private ISymbolTable GetTableOrThrow(NodeBase node)
    {
        var table = _symbolTables.FirstOrDefault(t => t.Contains(node));
        if (table == null) throw new ArgumentException();
        return table;
    }
}