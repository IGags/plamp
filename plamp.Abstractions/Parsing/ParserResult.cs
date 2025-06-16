using System.Collections.Generic;
using plamp.Abstractions.Ast;
using plamp.Abstractions.Ast.Node;

namespace plamp.Abstractions.Parsing;

public record ParserResult(
    List<NodeBase> NodeList, 
    IReadOnlyList<PlampException> Exceptions, 
    ISymbolTable SymbolTable);