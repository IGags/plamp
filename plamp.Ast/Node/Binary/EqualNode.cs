namespace plamp.Ast.Node.Binary;

public record EqualNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);