namespace plamp.Ast.Node.Binary;

public record BitwiseOrNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);