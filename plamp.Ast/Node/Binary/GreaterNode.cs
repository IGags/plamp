namespace plamp.Ast.Node.Binary;

public record GreaterNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);