namespace plamp.Ast.Node.Unary;

public record PostfixIncrementNode(NodeBase Inner) : UnaryBaseNode(Inner);