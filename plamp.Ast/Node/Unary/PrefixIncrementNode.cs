namespace plamp.Ast.Node.Unary;

public record PrefixIncrementNode(NodeBase Inner) : UnaryBaseNode(Inner);