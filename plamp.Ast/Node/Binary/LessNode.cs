namespace plamp.Ast.Node.Binary;

public record LessNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);