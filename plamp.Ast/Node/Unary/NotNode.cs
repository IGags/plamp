namespace plamp.Ast.Node.Unary;

public record NotNode(NodeBase Inner) : UnaryBaseNode(Inner);