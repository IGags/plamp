namespace plamp.Ast.Node.Binary;

public record LessOrEqualNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);