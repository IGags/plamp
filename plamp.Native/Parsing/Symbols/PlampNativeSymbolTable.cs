using System;
using System.Collections.Generic;
using System.Reflection;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Native.Parsing.Symbols;

/// <inheritdoc />
internal class PlampNativeSymbolTable : ISymbolTable
{
    private readonly Dictionary<NodeBase, PlampNativeSymbolRecord> _symbols;

    internal PlampNativeSymbolTable(Dictionary<NodeBase, PlampNativeSymbolRecord> symbols)
    {
        _symbols = symbols;
    }
    
    public PlampException SetExceptionToNodeAndChildren(
        PlampExceptionRecord exceptionRecord,
        NodeBase node,
        string fileName,
        AssemblyName assemblyName)
    {
        if (node.SymbolOverride != null 
            && node.SymbolOverride.TryOverride(exceptionRecord, out var exception))
        {
            return exception;
        }
        
        var symbolStack = new Stack<NodeBase>();
        symbolStack.Push(node);
        var positionMinimum = new FilePosition(int.MaxValue, int.MaxValue);
        var positionMaximum = new FilePosition(int.MinValue, int.MinValue);
        
        while (symbolStack.Count > 0)
        {
            if (!_symbols.TryGetValue(node, out var plampNativeSymbolRecord))
            {
                throw new Exception("Symbol is not found in symbol table.");
            }

            foreach (var token in plampNativeSymbolRecord.Tokens)
            {
                if (token.Start < positionMinimum)
                {
                    positionMinimum = token.Start;
                }

                if (token.End > positionMaximum)
                {
                    positionMaximum = token.End;
                }
            }
            
            foreach (var child in plampNativeSymbolRecord.Children)
            {
                symbolStack.Push(child);
            }
        }

        return new PlampException(exceptionRecord, positionMinimum, positionMaximum, fileName, assemblyName);
    }

    public PlampException SetExceptionToNodeWithoutChildren(
        PlampExceptionRecord exceptionRecord, 
        NodeBase node,
        string fileName,
        AssemblyName assemblyName)
    {
        if (node.SymbolOverride != null 
            && node.SymbolOverride.TryOverride(exceptionRecord, out var exception))
        {
            return exception;
        }
        
        if (!_symbols.TryGetValue(node, out var plampNativeSymbolRecord))
        {
            throw new Exception("Symbol is not found in symbol table.");
        }

        var positionMinimum = new FilePosition(int.MaxValue, int.MaxValue);
        var positionMaximum = new FilePosition(int.MinValue, int.MinValue);
        
        foreach (var token in plampNativeSymbolRecord.Tokens)
        {
            if (token.Start < positionMinimum)
            {
                positionMinimum = token.Start;
            }

            if (token.End > positionMaximum)
            {
                positionMaximum = token.End;
            }
        }
        return new PlampException(exceptionRecord, positionMinimum, positionMaximum, fileName, assemblyName);
    }

    public List<PlampException> SetExceptionToChildren(
        PlampExceptionRecord exceptionRecord,
        NodeBase node,
        string fileName,
        AssemblyName assemblyName)
    {
        if (!_symbols.TryGetValue(node, out var plampNativeSymbolRecord))
        {
            throw new Exception("Symbol is not found in symbol table.");
        }

        var childExceptions = new List<PlampException>();
        
        foreach (var child in plampNativeSymbolRecord.Children)
        {
            if (child.SymbolOverride != null
                && node.SymbolOverride.TryOverride(exceptionRecord, out var exception))
            {
                childExceptions.Add(exception);
                continue;
            }
            
            var ex = SetExceptionToNodeAndChildren(exceptionRecord, child, fileName, assemblyName);
            childExceptions.Add(ex);
        }

        return childExceptions;
    }

    public bool Contains(NodeBase node)
    {
        return _symbols.ContainsKey(node);
    }

    public bool TryGetChildren(NodeBase node, out IReadOnlyList<NodeBase> children)
    {
        if (_symbols.TryGetValue(node, out var value))
        {
            children = value.Children;
            return true;
        }

        children = null;
        return false;
    }
}