namespace plamp.Ast.Node.Binary;

public record DivideNode(NodeBase Left, NodeBase Right) : BaseBinaryNode(Left, Right);