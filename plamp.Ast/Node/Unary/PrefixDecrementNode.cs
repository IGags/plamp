namespace plamp.Ast.Node.Unary;

public record PrefixDecrementNode(NodeBase Inner) : UnaryBaseNode(Inner);