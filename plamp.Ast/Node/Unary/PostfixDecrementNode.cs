namespace plamp.Ast.Node.Unary;

public record PostfixDecrementNode(NodeBase Inner) : UnaryBaseNode(Inner);