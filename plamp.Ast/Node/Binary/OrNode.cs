namespace plamp.Ast.Node.Binary;

public record OrNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);