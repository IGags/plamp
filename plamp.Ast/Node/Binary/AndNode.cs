namespace plamp.Ast.Node.Binary;

public record AndNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);