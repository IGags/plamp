namespace plamp.Ast.Node.Binary;

public record ModuloNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);