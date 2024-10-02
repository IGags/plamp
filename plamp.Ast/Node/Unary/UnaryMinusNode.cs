namespace plamp.Ast.Node.Unary;

public record UnaryMinusNode(NodeBase Inner) : UnaryBaseNode(Inner);