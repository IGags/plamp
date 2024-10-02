namespace plamp.Ast.Node.Binary;

public record MinusNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);