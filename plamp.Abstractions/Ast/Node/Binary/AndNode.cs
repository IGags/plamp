namespace plamp.Abstractions.Ast.Node.Binary;

public class AndNode(NodeBase left, NodeBase right) : BaseBinaryNode(left, right);