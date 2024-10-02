namespace plamp.Ast.Node.Binary;

public record GreaterOrEqualsNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);