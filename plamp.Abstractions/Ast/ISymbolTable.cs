using System.Collections.Generic;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Ast;

/// <summary>
/// Symbol table that matches node from ast and position in source code
/// Should be generated by parser for compiler and visitors
/// If node does not exist it should throw exception
/// </summary>
public interface ISymbolTable
{
    /// <summary>
    /// Sets exception to node in symbol table
    /// </summary>
    PlampException SetExceptionToNode(NodeBase node, PlampExceptionRecord exceptionRecord, string fileName);

    /// <summary>
    /// Sets exception in min file position and max position of list
    /// </summary>
    PlampException SetExceptionToNodeRange(List<NodeBase> nodes, PlampExceptionRecord exceptionRecord, string fileName);
    
    bool TryGetSymbol(NodeBase symbol, out KeyValuePair<FilePosition, FilePosition> pair);

    void AddSymbol(NodeBase symbol, FilePosition start, FilePosition end);
}