using plamp.Ast;
using plamp.Ast.Node;

namespace plamp.Validators.Models;

public record ValidationContext(ISymbolTable Table, NodeBase Ast);