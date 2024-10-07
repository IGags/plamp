namespace plamp.Ast.Node.Binary;

public record XorNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);