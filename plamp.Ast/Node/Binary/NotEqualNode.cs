namespace plamp.Ast.Node.Binary;

public record NotEqualNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);