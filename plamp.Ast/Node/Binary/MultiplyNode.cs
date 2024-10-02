namespace plamp.Ast.Node.Binary;

public record MultiplyNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);