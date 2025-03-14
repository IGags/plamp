using plamp.Ast.Node;

namespace plamp.Ast.Modules;

public interface IWritableMember
{
    public NodeBase AstRepresentation { get; }
    
    public ISymbolTable DefinitionSymbolTable { get; }
}