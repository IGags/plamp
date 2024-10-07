namespace plamp.Ast.Node.Binary;

public record BitwiseAndNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);