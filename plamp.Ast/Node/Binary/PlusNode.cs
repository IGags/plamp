namespace plamp.Ast.Node.Binary;

public record PlusNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);